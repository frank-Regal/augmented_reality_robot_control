using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Microsoft;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.UI;

using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    [RequireComponent(typeof(RosConnector))]
    
    public class HandCtrlPoseStampedPub : MonoBehaviour
    {
        /****************************************************
         * Initialize Variables
         */

        public const string FrameId = "Unity";           // Set reference frame id 
        public string ROSTopicName;                      // Set ROS topic name
        public GameObject RobotBodyFrame;                // Reference to body frame of the robot you are controlling
        public bool ControlRobot;                        // TEMP: fix for turning on/off robot control
        public Handedness Hand;                          // Choose which hand to control the robot with

        protected MessageTypes.Geometry.PoseStamped msg; // ROS topic message type
        protected RosConnector msg_comp;                 // ROS message component
        protected MixedRealityPose index_pose;           // Output of MRTK utility function
        protected MixedRealityPose thumb_pose;           // Output of MRTK utility function
        protected float pinch_value;                     // Value used to determine if robot is grabbed
        protected string publicationId;                  // ROS message publication ID used for publishing
        protected Transform robot_tf;                    // Robot space frame origin
        protected Vector3 new_eof_position =             // New end effector position (Position vector that is published out)
            new Vector3(0, 0, 0);
        protected Quaternion new_eof_rotation =          // [NOT USED] New end effector rotation (Rotation quat that is published out) 
            new Quaternion(0, 0, 0, 0);
        protected Vector3 index_tip_position =           // Index finger tip position
            new Vector3(0, 0, 0);
        protected Quaternion index_tip_rotation =        // Index finger tip rotation
            new Quaternion(0, 0, 0, 0);


        /****************************************************
         * Helper functions
         */

        // Depreciated
        public void EnableControlRobot(Interactable myInteractable)
        {
            // Get whether the Interactable is selected or not
            ControlRobot = myInteractable.IsToggled;
        }

        // Logic to enable/disable hand control (publishing)
        // when scaling the size of the robot in the scene
        public void ControlRobotWhenScaling(bool IsEnabled)
        {
            ControlRobot = IsEnabled;
        }

        // Convert to ROS geo point msg
        protected MessageTypes.Geometry.Point GetGeometryPoint(Vector3 position)
        {
            MessageTypes.Geometry.Point geometryPoint = new MessageTypes.Geometry.Point();
            geometryPoint.x = position.x;
            geometryPoint.y = position.y;
            geometryPoint.z = position.z;
            return geometryPoint;
        }

        // Convert to ROS quaternion msg
        protected MessageTypes.Geometry.Quaternion GetGeometryQuaternion(Quaternion quaternion)
        {
            MessageTypes.Geometry.Quaternion geometryQuaternion = new MessageTypes.Geometry.Quaternion();
            geometryQuaternion.x = quaternion.x;
            geometryQuaternion.y = quaternion.y;
            geometryQuaternion.z = quaternion.z;
            geometryQuaternion.w = quaternion.w;
            return geometryQuaternion;
        }

        // Reference: MRTK
        // <summary>
        // Pinch calculation of the index finger with the thumb based on the distance between the finger tip and the thumb tip.
        // 4 cm (0.04 unity units) is the treshold for fingers being far apart and pinch being read as 0.
        // </summary>
        // <param name="handedness">Handedness to query joint pose against.</param>
        // <returns> Float ranging from 0 to 1. 0 if the thumb and finger are not pinched together, 1 if thumb finger are pinched together</returns>
        private const float IndexThumbSqrMagnitudeThreshold = 0.0016f;
        public static float CalculateIndexPinch(MixedRealityPose indexPose, MixedRealityPose thumbPose)
        {
            Vector3 distanceVector = indexPose.Position - thumbPose.Position;
            float indexThumbSqrMagnitude = distanceVector.sqrMagnitude;

            float pinchStrength = Mathf.Clamp(1 - indexThumbSqrMagnitude / IndexThumbSqrMagnitudeThreshold, 0.0f, 1.0f);
            return pinchStrength;
        }


        /****************************************************
         * Publish PoseStamped messages
         */

        // Main function. Called from FixedUpdate().
        // Finds hand transforms and publishes those messages on a PoseStamped topic
        protected virtual void PublishPoseStamped()
        {
            // Currently using the pinch between the index finger tip and the thumb
            // as the indicator to start to publish PoseStamped messages

            // Check if hands are being tracked, if not do nothing.
            // If tracked fill index_pose and thumb_pose with the world position
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Hand, out index_pose) &&
                HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Hand, out thumb_pose))
            {
                // Make a copy from MRTK Vector3 to Unity Vector3 (not ideal)
                index_tip_position = index_pose.Position;
                index_tip_rotation = index_pose.Rotation;

                // Calculate whether or not index tip and thumb tip are pinching
                pinch_value = CalculateIndexPinch(index_pose, thumb_pose);
                
                // If hand is pinching, transform hand positions to robot space frame
                // and publish on PoseStamped. Not pinching, do nothing.
                if (pinch_value >= 0.89f) // tuned value found from debug logs
                {
                    // Get body frame transformation of robot
                    robot_tf = RobotBodyFrame.transform;
                    
                    // Transform finger tip position into robot ref frame
                    new_eof_position = robot_tf.InverseTransformPoint(index_tip_position);
                    
                    // Fill PoseStamped ROS message
                    msg.header.Update(); 
                    msg.pose.position = GetGeometryPoint(new_eof_position.Unity2Ros()); // convert to ros style

                    // *Note: 
                    // Left and Right Hand Orientation is not used in the current vr_baxter_demo scripts on the robot.
                    // Orientation values are hard coded in the UnityController.py & UnityControllerRight.py files
                    // scripts located in the following directories in the catkin_ws on the robot
                    // (~/catkin_ws/src/vr_baxter/src/)
                    msg.pose.orientation = GetGeometryQuaternion(index_tip_rotation.Unity2Ros()); // convert to ros style

                    // Send ROS PoseStamped msg
                    msg_comp.RosSocket.Publish(publicationId, msg);
                }
            }
        }

        
        /****************************************************
         * Initialization calls
         */

        // Initialize ROS PoseStamped message
        private void InitializePoseStampedMsg()
        {
            msg = new MessageTypes.Geometry.PoseStamped
            {
                header = new MessageTypes.Std.Header
                {
                    frame_id = FrameId
                }
            };
        }

        // Start is called before the first frame update
        protected void Start()
        {
            Time.fixedDeltaTime = 0.2f;                    // [1hz] Control FixedUpdate publishing rates
            InitializePoseStampedMsg();                    // Create new pose stamped message
            msg_comp = GetComponent<RosConnector>();       // Get ros connector component
            publicationId = msg_comp.RosSocket.            // Start advertising ROS message
                Advertise<MessageTypes.Geometry.
                PoseStamped>(ROSTopicName);
        }


        /****************************************************
         * Constant call
         */

        // FixedUpdate is called at rate specified in Start()
        protected void FixedUpdate()
        {
            //Debug.Log(ControlRobot);
            if (ControlRobot)
            {
                // Call to publish hand locations over the PoseStamped Topic
                PublishPoseStamped();
            }
        }
    }
}
