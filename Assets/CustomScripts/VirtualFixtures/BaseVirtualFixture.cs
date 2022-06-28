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

    protected MeshRenderer mesh_comp;          // mesh render component used for the virtual fixture
    protected Mesh mesh_filter_comp;           // mesh filter component used for the virtual fixture mesh calcs
    protected Transform mesh_trans_comp;       // mesh transform component used for the virtual fixture
    protected Material default_material;       // material that is assigned to the virtual fixture by default
    protected MeshCollider mesh_collider_comp; // mesh collider component attached to the virtual fixture
    protected HandCtrlPoseStampedPub ros_comp; // ros connection component  

    private Vector3 io_position;               // interaction object world postion
    private Vector3 closest_vf_point;          // point on the virtual fixture the interaction object is closest to
    private float vf_to_io_dist;               // distance that the interaction object is from the closest point on the virtual fixture
    private bool IsObjectInteracting;          // boolean used to assign true or false based on if interaction object is colliding with virtual fixture
    private float dist = 0;                    // distance variable used for getting closest vertices 
    private float first_dist = float.PositiveInfinity; 
    private float sec_dist = float.PositiveInfinity;
    private Vector3[] mesh_vertices;

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
    
    private void GetInteractionStatus()
    {
        // Get the closest point from the interaction object to the virtual fixture
        GetClosestPoint(ref closest_vf_point);

        // distance that the interaction object is from the closest point on the virtual fixture
        vf_to_io_dist = (io_position - closest_vf_point).magnitude;

        // Debug
        // Debug.Log(vf_to_io_dist);

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
    }

    protected void ChangeMaterialOnContact()
    {
        // Call to get interaction status
        GetInteractionStatus();

        // Assign appropriate material
        if (IsObjectInteracting)
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

    private void GetNormal(Vector3 closest_point, ref Vector3 normal_vector)
    {
        // Init
        Vector3 out_point_one = new Vector3(0f, 0f, 0f); // closest vector 1
        Vector3 out_point_two = new Vector3(0f, 0f, 0f); // closest vector 2
        Vector3 vec_a = new Vector3(0f, 0f, 0f);
        Vector3 vec_b = new Vector3(0f, 0f, 0f);

        // Get World transformation matrix of Virtual Fixture
        Matrix4x4 vf_tf = mesh_trans_comp.transform.localToWorldMatrix;

        // Get vertex location of all vertices on the mesh
        mesh_vertices = mesh_filter_comp.vertices;

        for (int i = 0; i < mesh_vertices.Length; i++)
        {
            // TODO 
        }

        // Find two vertices close to the closest_point point of contact
        GetTwoClosestVertices(ref mesh_vertices, ref closest_point, ref out_point_one, ref out_point_two);

        // Create vectors and cross for normal vector
        vec_a = closest_point - out_point_one;
        vec_b = closest_point - out_point_two;
        normal_vector = Vector3.Cross(vec_a, vec_b);



    }

    private void GetTwoClosestVertices(ref Vector3[] vertices,
        ref Vector3 closest_point,
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

