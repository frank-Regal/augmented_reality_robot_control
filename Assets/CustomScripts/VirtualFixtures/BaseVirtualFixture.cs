using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.UI;

using RosSharp.RosBridgeClient;

using UnityEngine;

public enum InteractionType
{
    GameObject,
    PinchedFingers
}

// Base Class for Virtual Fixtures in Augmented Reality
public class BaseVirtualFixture : MonoBehaviour
{
    /*
    / Actions
    */
    public static event Action OnVirtualFixturesAddedToScene;         // broadcast if virtual fixtures are in the scene
    public static event Action<bool> OnContactMadeWithVirtualFixture; // broadcast status of contact with the virtual fixture
    public static event Action<Vector3> SendContactLocation;          // broadcast location of contact


    /*
    / Initialize
    */
    public GameObject RosConnectionObject;     // get the ros connection manager used to pub and sub to robot
    public InteractionType InteractionObjectType;
    public Handedness InteractionHand;         // Choose which hand to control the robot with
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

    private bool IsObjectInteracting = false;          // boolean used to assign true or false based on if interaction object is colliding with virtual fixture
    private bool IsObjectStillInteracting = false;     // boolean used to 
    private float dist = 0;                    // distance variable used for getting closest vertices 
    private float first_dist =                 // used for finding closest point to contact location
        float.PositiveInfinity; 
    private float sec_dist =                   // used for finding second closest point to contact location
        float.PositiveInfinity;
    //private Vector3 io_position;               // interaction object world postion
    
    private Vector3[] mesh_vertices;           // vector array of vertex locations of the mesh in the world frame
    private Vector3[] vertex_normals;          // array of normal vectors to the mesh vertex location
    private Matrix4x4 Tf_mesh_vertex;          // transformation matrix for the vertices located on the mesh
    private Matrix4x4 tf_normal;
    private float vf_to_io_dist = float.PositiveInfinity;      // distance that the interaction object is from the closest point on the virtual fixture

    private bool IsObjectInteractionActive = false;

    
    /****************************************************
    / Hand Interaction Methods
    */

    // Helper function for hand tracking
    private const float IndexThumbSqrMagnitudeThreshold = 0.0016f;
    public static float CalculateIndexPinch(MixedRealityPose indexPose, MixedRealityPose thumbPose)
    {
        Vector3 distanceVector = indexPose.Position - thumbPose.Position;
        float indexThumbSqrMagnitude = distanceVector.sqrMagnitude;

        float pinchStrength = Mathf.Clamp(1 - indexThumbSqrMagnitude / IndexThumbSqrMagnitudeThreshold, 0.0f, 1.0f);
        return pinchStrength;
    }


    // Implementation for Hand Tracking
    public float GetDistanceBetweenHandsAndVf()
    {
        MixedRealityPose index_pose;           // Output of MRTK utility function
        MixedRealityPose thumb_pose;           // Output of MRTK utility function
        float pinch_value;                     // index tip and thumb seperation distance
        Vector3 index_tip_position;            // vector to hold the postiion of the index finger pose
        Vector3 closest_point;

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, InteractionHand, out index_pose) &&
                HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, InteractionHand, out thumb_pose))
        {
            // Make a copy from MRTK Vector3 to Unity Vector3(not ideal)
            index_tip_position = index_pose.Position;

            // Calculate whether or not index tip and thumb tip are pinching
            pinch_value = CalculateIndexPinch(index_pose, thumb_pose);

            if (pinch_value >= 0.89f) // tuned value found from debug logs
            {
                // point on the virtual fixture the interaction object is closest to
                closest_point = mesh_collider_comp.ClosestPoint(index_tip_position);
                
                // distance that the interaction object is from the closest point on the virtual fixture
                vf_to_io_dist = (index_tip_position - closest_point).magnitude;
            }
        }

        return vf_to_io_dist;
    }


    /****************************************************
    / Trigger Methods to be called from child classes
    */
    protected bool GetInteractionStatus()
    {
        // Init
        Vector3 closest_vf_point;    // point on the virtual fixture the interaction object is closest to
        

        // Check if the user wants to interact with the virtual surface with their hands or a game object
        if (InteractionObjectType.Equals(InteractionType.PinchedFingers))
        {
            vf_to_io_dist = GetDistanceBetweenHandsAndVf();
        }
        else
        {
            // interaction object world postion
            Vector3 io_position = InteractionObject.transform.position;

            // point on the virtual fixture the interaction object is closest to
            closest_vf_point = mesh_collider_comp.ClosestPoint(io_position);

            // distance that the interaction object is from the closest point on the virtual fixture
            vf_to_io_dist = (io_position - closest_vf_point).magnitude;
        }

        Debug.Log(vf_to_io_dist);
        // Value based determination (tuned value)
        if (vf_to_io_dist < 0.19) // 0.002
        {
            IsObjectInteracting = true;
            OnContactMadeWithVirtualFixture?.Invoke(IsObjectInteracting); // broadcast
            //SendContactLocation?.Invoke(closest_vf_point); // broadcast
        }
        else
        {
            IsObjectInteracting = false;
            OnContactMadeWithVirtualFixture?.Invoke(IsObjectInteracting); // broadcast
        }

        return IsObjectInteracting;
    }
    
    protected void HoldInteractionStatus(in bool IsContactMade)
    {
        if (IsContactMade == true && IsObjectStillInteracting == false)
        {
            IsObjectStillInteracting = true;
        } 
        else if (IsContactMade == false && IsObjectStillInteracting == true)
        {
            IsObjectStillInteracting = true;
        } 
        else if (IsContactMade == true && IsObjectStillInteracting == true)
        {
            IsObjectStillInteracting = false;
        }

        if (IsObjectStillInteracting == true)
        {
            ChangeMaterialOnContact(true);
        }
        else if (IsObjectStillInteracting == false)
        {
            ChangeMaterialOnContact(false);
        }
           
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

        }
    }
    
    /****************************************************
    / Get Normal to Virtual Fixture Closest Point
    */
    private void GetNormal(ref Vector3 normal_vector)
    {
        // Init
        Vector3 closest_vf_point = new Vector3(0f,0f,0f); // point on the virtual fixture the interaction object is closest to
        //float vf_to_io_dist = 0;    
        Vector3 out_point_one = new Vector3(0f, 0f, 0f);  // closest vector 1
        Vector3 out_point_two = new Vector3(0f, 0f, 0f);  // closest vector 2
        Vector3 vec_a;
        Vector3 vec_b;

        // Check if the user wants to interact with the virtual surface with their hands or a game object
        if (InteractionObjectType.Equals(InteractionType.PinchedFingers))
        {
            GetDistanceBetweenHandsAndVf();
        }
        else
        {
            // interaction object world postion
            Vector3 io_position = InteractionObject.transform.position;

            // point on the virtual fixture the interaction object is closest to
            closest_vf_point = mesh_collider_comp.ClosestPoint(io_position);
        }
             
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
        normal_vector = Vector3.Cross(vec_b, vec_a);

        // Add the translational component to the transformation matrix for the normal vector
        for (int i = 0; i < 3; i++)
        {
            tf_normal[i, 3] = closest_vf_point[i];
        }

        // Scale Normal Vector
        normal_vector = normal_vector.normalized*5f;
        //Debug.Log(normal_vector);

        // Rotate and translate normal vector
        normal_vector = tf_normal.MultiplyPoint3x4(normal_vector);

        NormalVectorObject.SetActive(true);

        Instantiate(NormalVectorObject, closest_vf_point, mesh_rotation);
        
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


    /****************************************************
    / Start is called before the first frame update
    */
    protected void Start()
    {

        // Tell all listeners Virtual Fixtures are in the scene
        OnVirtualFixturesAddedToScene?.Invoke(); // broadcast

        // Initalize mesh renderer compoennt of virtual fixture
        mesh_comp = GetComponent<MeshRenderer>();
        mesh_filter_comp = GetComponent<MeshFilter>().mesh;
        mesh_trans_comp = GetComponent<Transform>();

        //Initialize mesh collider component of virtual fixture
        mesh_collider_comp = GetComponent<MeshCollider>();

        // Initialize hand ctrl pose stamped pub 
        ros_comp = RosConnectionObject.GetComponent<HandCtrlPoseStampedPub>();

        // Store the default material set to the mesh component of the virtual fixture
        default_material = mesh_comp.material;

    }
}

