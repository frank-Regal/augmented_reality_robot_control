using System;
using System.Collections;
using System.Collections.Generic;
using RosSharp.RosBridgeClient;
using UnityEngine;


// Base Class for Virtual Fixtures in Augmented Reality
public class BaseVirtualFixture : MonoBehaviour
{
    /*
    / Initialize
    */
    public static event Action OnVirtualFixturesAddedToScene;         // broadcast if virtual fixtures are in the scene
    public static event Action<bool> OnContactMadeWithVirtualFixture; // broadcast status of contact with the virtual fixture
    public static event Action<Vector3> SendContactLocation;          // broadcast location of contact

    public GameObject RosConnectionObject;     // get the ros connection manager used to pub and sub to robot
    public GameObject VirtualFixture;          // the game object for the virtual fixture
    public GameObject InteractionObject;       // the game object you want the VF to interact with
    public Material OnContactMaterial;         // what color to change virtual fixture to

    // Debugging, comment out for production
    public GameObject NormalVectorObject;      // used for visualization debugging
    private GameObject Temp;

    protected MeshRenderer mesh_comp;          // mesh render component used for the virtual fixture
    protected Mesh mesh_filter_comp;           // mesh filter component used for the virtual fixture mesh calcs
    protected Transform mesh_trans_comp;       // mesh transform component used for the virtual fixture
    protected Material default_material;       // material that is assigned to the virtual fixture by default
    protected MeshCollider mesh_collider_comp; // mesh collider component attached to the virtual fixture (YOU NEED TO ADD THIS AS A COMPONENT)
    protected HandCtrlPoseStampedPub ros_comp; // ros connection component

    private bool IsObjectInteracting;          // boolean used to assign true or false based on if interaction object is colliding with virtual fixture
    private float vf_to_io_dist;               // distance that the interaction object is from the closest point on the virtual fixture
    private float dist = 0;                    // distance variable used for getting closest vertices 
    private float first_dist =                 // used for finding closest point to contact location
        float.PositiveInfinity; 
    private float sec_dist =                   // used for finding second closest point to contact location
        float.PositiveInfinity;
    private Vector3 io_position;               // interaction object world postion
    private Vector3 closest_vf_point;          // point on the virtual fixture the interaction object is closest to
    private Vector3[] mesh_vertices;           // vector array of vertex locations of the mesh in the world frame
    private Vector3[] vertex_normals;          // array of normal vectors to the mesh vertex location
    private Matrix4x4 Tf_mesh_vertex;          // transformation matrix for the vertices located on the mesh
    private Matrix4x4 tf_normal;

    /*
    / Debug Functions
    */
    protected void PrintVirtualFixturePose()
    {
        Debug.Log("[" + VirtualFixture.name + " Pose]:" +
                  " X: " + VirtualFixture.transform.position.x.ToString() +
                  "; Y: " + VirtualFixture.transform.position.y.ToString() +
                  "; Z: " + VirtualFixture.transform.position.z.ToString() +
                  "; Rot X: " + VirtualFixture.transform.rotation.x.ToString() +
                  "; Rot Y: " + VirtualFixture.transform.rotation.y.ToString() +
                  "; Rot Z: " + VirtualFixture.transform.rotation.z.ToString() +
                  "; Rot W: " + VirtualFixture.transform.rotation.w.ToString());
    }

    protected void PrintInteractionObjectPose()
    {
        Debug.Log("[" + InteractionObject.name + " Pose]:" +
                  " X: " + InteractionObject.transform.position.x.ToString() +
                  "; Y: " + InteractionObject.transform.position.y.ToString() +
                  "; Z: " + InteractionObject.transform.position.z.ToString() +
                  "; Rot X: " + InteractionObject.transform.rotation.x.ToString() +
                  "; Rot Y: " + InteractionObject.transform.rotation.y.ToString() +
                  "; Rot Z: " + InteractionObject.transform.rotation.z.ToString() +
                  "; Rot W: " + InteractionObject.transform.rotation.w.ToString());
    }

    /*
    / Location Calculations
    */
    public void GetClosestPoint(ref Vector3 closest_point)
    {
        // interaction object world postion
        io_position = InteractionObject.transform.position;

        // point on the virtual fixture the interaction object is closest to
        closest_point = mesh_collider_comp.ClosestPoint(io_position);
    }
    
    protected bool GetInteractionStatus()
    {
        // Get the closest point from the interaction object to the virtual fixture
        GetClosestPoint(ref closest_vf_point);

        // distance that the interaction object is from the closest point on the virtual fixture
        vf_to_io_dist = (io_position - closest_vf_point).magnitude;

        // Debug
        //Debug.Log(vf_to_io_dist);

        // Value based determination (tune value)
        if (vf_to_io_dist < 0.002)
        {
            IsObjectInteracting = true;
            OnContactMadeWithVirtualFixture?.Invoke(IsObjectInteracting); // broadcast
            SendContactLocation?.Invoke(closest_vf_point); // broadcast
        }
        else
        {
            IsObjectInteracting = false;
            OnContactMadeWithVirtualFixture?.Invoke(IsObjectInteracting); // broadcast
        }

        return IsObjectInteracting;
    }

    protected void ChangeMaterialOnContact(in bool IsInteracting)
    {
        // Assign appropriate material
        if (IsInteracting)
        {
            mesh_comp.material = OnContactMaterial;
        }
        else
        {
            mesh_comp.material = default_material;
        }
    }

    /*
    / Get Normal to Virtual Fixture Closest Point
    */

    protected void GetNormalVectorAtContactPoint(in bool IsInteracting, ref Vector3 normal_vector)
    {
        // Assign appropriate material
        if (IsInteracting)
        {
            GetNormal(ref normal_vector);
            Debug.Log("Made call to Get Normal");
        }
        else
        {
            //GameObject.Find("NormalVisualiztion").SetActive(false);
        }
    }

    private void GetNormal(ref Vector3 normal_vector)
    {
        // Init
        Vector3 out_point_one = new Vector3(0f, 0f, 0f); // closest vector 1
        Vector3 out_point_two = new Vector3(0f, 0f, 0f); // closest vector 2
        Vector3 vec_a = new Vector3(0f, 0f, 0f);
        Vector3 vec_b = new Vector3(0f, 0f, 0f);

        // Get World transformation matrix of Virtual Fixture
        Matrix4x4 vf_tf = mesh_trans_comp.transform.localToWorldMatrix;
        
        // Only used for visualization, remove for production
        Quaternion mesh_rotation = mesh_trans_comp.transform.rotation;

        // Fill transformation matrix for the normal vector with
        // just the same rotational component that the mesh object has
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                tf_normal[i, j] = vf_tf[i, j];
            }
        }

        // Get vertex locations of all vertices on the mesh in local coordinate frame
        mesh_vertices = mesh_filter_comp.vertices;

        for (int i = 0; i < mesh_vertices.Length; i++)
        {
            // Rotate and Translate each vertex of the mesh to world coordinates
            mesh_vertices[i] = vf_tf.MultiplyPoint3x4(mesh_vertices[i]);
        }

        // Find the two closest vertices to the closest_point point of contact on the mesh
        GetTwoClosestVertices(mesh_vertices, closest_vf_point, ref out_point_one, ref out_point_two);

        // Get normal vector to the closest_point point of contact on the mesh
        vec_a = closest_vf_point - out_point_one;
        vec_b = closest_vf_point - out_point_two;
        normal_vector = Vector3.Cross(vec_a, vec_b);

        // Add the translational component to the transformation matrix for the normal vector
        for (int i = 0; i < 3; i++)
        {
            tf_normal[i, 3] = closest_vf_point[i];
        }

        // Rotate and translate normal vector
        normal_vector = tf_normal.MultiplyPoint3x4(normal_vector.normalized);
        NormalVectorObject.SetActive(true);
        Instantiate(NormalVectorObject, normal_vector, mesh_rotation);
    }


    // When closest point is found, this method finds the two closest vertices on the mesh
    private void GetTwoClosestVertices(in Vector3[] vertices,
                                       in Vector3 closest_point,
                                       ref Vector3 out_point_one,
                                       ref Vector3 out_point_two)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            dist = (vertices[i] - closest_point).magnitude;
            if (dist < first_dist)
            {
                out_point_one = vertices[i];
                first_dist = dist;
            }
            else if (dist < sec_dist)
            {
                out_point_two = vertices[i];
                sec_dist = dist;
            }
        }
    }


    /*
    / Start is called before the first frame update
    */
    protected void Start()
    {

        // Tell all listeners Virtual Fixtures are in the scene
        OnVirtualFixturesAddedToScene?.Invoke(); // broadcast

        // Initalize mesh renderer compoennt of virtual fixture
        mesh_comp = VirtualFixture.GetComponent<MeshRenderer>();
        mesh_filter_comp = VirtualFixture.GetComponent<MeshFilter>().mesh;
        mesh_trans_comp = VirtualFixture.GetComponent<Transform>();

        //Initialize mesh collider component of virtual fixture
        mesh_collider_comp = VirtualFixture.GetComponent<MeshCollider>();

        // Initialize hand ctrl pose stamped pub 
        ros_comp = RosConnectionObject.GetComponent<HandCtrlPoseStampedPub>();

        // Store the default material set to the mesh component of the virtual fixture
        default_material = mesh_comp.material;

    }
}

