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
    public class PalmCtrlPoseStampedPub : MonoBehaviour
    {
        // Initialize Variables
        public const string FrameId = "Unity";         // Set reference frame id 
        public string ROSTopicName;                    // Set ROS topic name
        public GameObject RobotBodyFrame;              // Reference to body frame of robot
        public bool ControlRobot;                      // TEMP fix for turning on/off robot control
        public Handedness Hand;                        // Choose which hand to control the robot with

        MessageTypes.Geometry.PoseStamped msg;         // ROS topic message type
        RosConnector msg_comp;                         // ROS message component
        string publicationId;                          // Name of publisher
        MixedRealityPose palm_pose;                   // Init output of MRTK utility functions
        Transform robot_tf;                            // Private baxter_body_frame
        Vector3 finger_pose_wrt_robot =                // Left hand index finger tip position
            new Vector3(0, 0, 0);
        Quaternion finger_rot_wrt_robot =              // Left hand index finger tip rotation
            new Quaternion(0, 0, 0, 0);
        Vector3 palm_position =                   // Left hand index finger tip position
            new Vector3(0, 0, 0);
        Quaternion palm_rotation =                // Left hand index finger tip rotation
            new Quaternion(0, 0, 0, 0);
        MessageTypes.Geometry.Point geo_point =        // ROS GeoPoint Msg
            new MessageTypes.Geometry.Point();
        MessageTypes.Geometry.Quaternion geo_quat =    // ROS Quaternion Msg
            new MessageTypes.Geometry.Quaternion();



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

        public void EnableControlRobot(Interactable myInteractable)
        {
            // Get whether the Interactable is selected or not
            ControlRobot = myInteractable.IsToggled;
        }

        public void ControlRobotWhenScaling(bool IsEnabled)
        {
            ControlRobot = IsEnabled;
        }

        // Convert to ROS geo point msg
        private MessageTypes.Geometry.Point GetGeometryPoint(Vector3 position)
        {
            MessageTypes.Geometry.Point geometryPoint = new MessageTypes.Geometry.Point();
            geometryPoint.x = position.x;
            geometryPoint.y = position.y;
            geometryPoint.z = position.z;
            return geometryPoint;
        }


        // Convert to ROS quaternion msg
        private MessageTypes.Geometry.Quaternion GetGeometryQuaternion(Quaternion quaternion)
        {
            MessageTypes.Geometry.Quaternion geometryQuaternion = new MessageTypes.Geometry.Quaternion();
            geometryQuaternion.x = quaternion.x;
            geometryQuaternion.y = quaternion.y;
            geometryQuaternion.z = quaternion.z;
            geometryQuaternion.w = quaternion.w;
            return geometryQuaternion;
        }


        /// <summary>
        /// Pinch calculation of the index finger with the thumb based on the distance between the finger tip and the thumb tip.
        /// 4 cm (0.04 unity units) is the treshold for fingers being far apart and pinch being read as 0.
        /// </summary>
        /// <param name="handedness">Handedness to query joint pose against.</param>
        /// <returns> Float ranging from 0 to 1. 0 if the thumb and finger are not pinched together, 1 if thumb finger are pinched together</returns>
        private const float IndexThumbSqrMagnitudeThreshold = 0.0016f;
        public static float CalculateIndexPinch(MixedRealityPose indexPose, MixedRealityPose thumbPose)
        {
            Vector3 distanceVector = indexPose.Position - thumbPose.Position;
            float indexThumbSqrMagnitude = distanceVector.sqrMagnitude;

            float pinchStrength = Mathf.Clamp(1 - indexThumbSqrMagnitude / IndexThumbSqrMagnitudeThreshold, 0.0f, 1.0f);
            return pinchStrength;
        }


        private void PublishPoseStamped()
        {
            // Check if hands are being tracked, if not do nothing
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Hand, out palm_pose))
            {
                // Make a copy from MRTK Vector3 to Unity Vector3 (not Ideal)
                palm_position = palm_pose.Position;
                palm_rotation = palm_pose.Rotation;

                // Debug
                //Debug.Log(Hand.ToString() + " Hand Pinch Value: " + pinch_value);

                // If hand is pinching calculate transform and publish pose stamped
                // msg of index finger wrt to robot body frame
                
                // Get body frame transformation of robot
                robot_tf = RobotBodyFrame.transform;

                // Transform finger tip position into robot ref frame
                palm_position = robot_tf.InverseTransformPoint(palm_position);

                // Fill ROS msg
                msg.header.Update();
                msg.pose.position = GetGeometryPoint(palm_position.Unity2Ros());
                msg.pose.orientation = GetGeometryQuaternion(palm_rotation.Unity2Ros());

                // Send ROS PoseStamped msg
                msg_comp.RosSocket.Publish(publicationId, msg);

                // Debug
                Debug.Log(Hand.ToString() + " Hand PoseStamped Message Published");
                
            }
        }


        // Start is called before the first frame update
        void Start()
        {
            //Application.targetFrameRate = 30;
            Time.fixedDeltaTime = 0.2f;                      // [1hz] Control FixedUpdate publishing rates
            InitializePoseStampedMsg();                    // Create new pose stamped message
            msg_comp = GetComponent<RosConnector>();       // Get ros connector component
            publicationId = msg_comp.RosSocket.            // Advertise ROS message
                Advertise<MessageTypes.Geometry.
                PoseStamped>(ROSTopicName);
        }


        // FixedUpdate is called at rate specified in Start()
        void FixedUpdate()
        {

            //Debug.Log(ControlRobot);
            if (ControlRobot)
            {
                // Main Function, gets transform and publishes msg
                PublishPoseStamped();
            }
        }
    }

}