using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Microsoft;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.UI;

using RosSharp.RosBridgeClient;

using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    public class VirtualFixtureHandCtrlPoseStampedPub : HandCtrlPoseStampedPub
    {

        /****************************************************
         * Initialize Variables
         */

        protected bool IsVirtualFixtureInScene = false;  // Boolean check to enable or disable virtual fixture logic
        protected bool ContactMade = false;              // Contact is made
        protected Vector3 VirtualFixtureLocation =       // Virtual fixture location
           new Vector3(0, 0, 0);



        

        /****************************************************
         * Delegate handlers and functions
         */

        // Enable delegate listeners
        private void OnEnable()
        {
            BaseVirtualFixture.OnVirtualFixturesAddedToScene += EnableVirtualFixtureControl;
            BaseVirtualFixture.OnContactMadeWithVirtualFixture += VirtualFixtureContactStatus;
            BaseVirtualFixture.SendContactLocation += VirtualFixtureContactLocation;
        }

        // Disable delegate listeners
        private void OnDisable()
        {
            BaseVirtualFixture.OnVirtualFixturesAddedToScene -= EnableVirtualFixtureControl;
            BaseVirtualFixture.OnContactMadeWithVirtualFixture -= VirtualFixtureContactStatus;
            BaseVirtualFixture.SendContactLocation -= VirtualFixtureContactLocation;
        }


        /****************************************************
         * Virtual Fixture calls
         */

        // Delegate callback
        void EnableVirtualFixtureControl()
        {
            IsVirtualFixtureInScene = true; // logic if virtual fixtures are in the scene
        }

        // Delegate callback
        void VirtualFixtureContactStatus(bool IsContactMade)
        {
            ContactMade = IsContactMade; // logic for determining if contact is made or not
        }

        // Delegate callback
        void VirtualFixtureContactLocation(Vector3 ContactLocation)
        {
            VirtualFixtureLocation = ContactLocation; // location that the hand made contact with the virtual fixture
        }

        private void TriggerVirtualFixtureLogic()
        {
            // Get location that the robot arm is making contact with the virtual fixture
            if (ContactMade)
            {
                // Transform the virtual fixture contact location to the robot ref frame
                new_eof_position = robot_tf.InverseTransformPoint(VirtualFixtureLocation);

                // Debug.Log("New EOF Position: " + new_eof_position.ToString());
            }
            else
            {
                // Transform finger tip position into robot space frame
                new_eof_position = robot_tf.InverseTransformPoint(index_tip_position);
            }
        }

        protected override void PublishPoseStamped()
        {
            // Check if hands are being tracked, if not do nothing.
            // If tracked fill index_pose and thumb_pose with the world position
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Hand, out index_pose) &&
                HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Hand, out thumb_pose))
            {
                // Make a copy from MRTK Vector3 to Unity Vector3 (not Ideal)
                index_tip_position = index_pose.Position;
                index_tip_rotation = index_pose.Rotation;

                // Calculate whether or not index tip and thumb tip are pinching
                pinch_value = CalculateIndexPinch(index_pose, thumb_pose);

                //Debug.Log(Hand.ToString() + " Hand Pinch Value: " + pinch_value);

                // If hand is pinching, transform hand positions to robot space frame and publish on PoseStamped
                // Not pinching, do nothing.
                if (pinch_value >= 0.89f) // hand is pinching (tuned value found from debug logs)
                {
                    // Get body frame transformation of robot
                    robot_tf = RobotBodyFrame.transform;

                    // virtual fixture in scene 
                    if (IsVirtualFixtureInScene)
                    {
                        // Call to Trigger Virtual Fixture Logic. Function is overwritten in child class
                        // VirtualFixtureHandCtrlPoseStampedPub.cs
                        TriggerVirtualFixtureLogic();
                    }
                    // no virtual fixture in scene 
                    else
                    {
                        // Transform finger tip position into robot ref frame
                        new_eof_position = robot_tf.InverseTransformPoint(index_tip_position);
                    }

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

                    /* // Debug Exactly what you are sending out as the message
                    Debug.Log(Hand.ToString() + " Hand PoseStamped Message Published");
                    Debug.Log("Position: X: " + msg.pose.position.x.ToString() + 
                                      "; Y: " + msg.pose.position.y.ToString() + 
                                      "; Z: " + msg.pose.position.z.ToString());
                    Debug.Log("Orientation: X: " + msg.pose.orientation.x.ToString() +
                                         "; Y: " + msg.pose.orientation.y.ToString() +
                                         "; Z: " + msg.pose.orientation.z.ToString() +
                                         "; W: " + msg.pose.orientation.w.ToString());
                    */
                }
            }
        }

        
        
    }

    
}
