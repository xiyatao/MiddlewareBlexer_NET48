using Kinect_Middleware.Scripts;
using Microsoft.Azure.Kinect.BodyTracking;
using System;
using System.Numerics;
using System.Runtime.Serialization;

namespace Kinect_Middleware.Models {
    /// <summary>
    /// MJoint which stands for Multiplatform Joint allows you to remove unwanted rotation 
    /// and normalize the values between kinect one and azure
    /// </summary>
    [DataContract]
    public class MJoint {
        [DataMember]
        public string name;
        [DataMember]
        public Vector3 position;
        [DataMember]
        public Quaternion rotation;

        private Quaternion OneToUnity(Quaternion oneQuaterion) {
            Quaternion rot = new Euler(0, 180, 0).ToQuaternion();

            Quaternion quaternion = new Quaternion(
                -oneQuaterion.X,
                -oneQuaterion.Y,
                 oneQuaterion.Z,
                 oneQuaterion.W
            );
            return quaternion * rot;
        }

        public MJoint(string name, Vector3 position, Quaternion oneRotation) {
            this.name = name;
            this.position = new Vector3(position.X, position.Y, -position.Z);
            this.rotation = OneToUnity(oneRotation);

            Quaternion rot_1 = new Euler(0, 0, -90).ToQuaternion();
            Quaternion rot_2 = new Euler(0, 0, 90).ToQuaternion();
            Quaternion rot_3 = new Euler(0, -90, 180).ToQuaternion();
            Quaternion rot_4 = new Euler(180, -90, 0).ToQuaternion();

            switch (name) {
                // No rotation
                case "SpineBase":
                case "SpineMid":
                case "SpineShoulder":
                case "Neck":
                case "Head":
                    break;
                // Left arm
                case "ShoulderLeft":
                case "ElbowLeft":
                case "WristLeft":
                case "HandLeft":
                    this.rotation *= rot_1;
                    break;
                // Right arm
                case "ShoulderRight":
                case "ElbowRight":
                case "WristRight":
                case "HandRight":
                    this.rotation *= rot_2;
                    break;
                // Left leg
                case "HipLeft":
                    this.rotation *= rot_1;
                    break;
                case "KneeLeft":
                case "AnkleLeft":
                    this.rotation *= rot_3;
                    break;
                // Right leg
                case "HipRight":
                    this.rotation *= rot_2;
                    break;
                case "KneeRight":
                case "AnkleRight":
                    this.rotation *= rot_4;
                    break;
                default:
                    throw new Exception("Joint not implemented");
            }
        }

        private Quaternion AzureToUnity(Quaternion azureQuaterion) {
            Quaternion rot = new Euler(0, 90, -90).ToQuaternion();
            Quaternion inverse = Quaternion.Inverse(rot);

            return azureQuaterion * inverse;
        }

        public MJoint(JointId joint, Vector3 position, Quaternion azureRotation) {
            Quaternion armLeftRotation = new Euler(0, 0, -90).ToQuaternion();
            Quaternion armRightRotation = new Euler(0, 180, -90).ToQuaternion();
            Quaternion legLeftRotation = new Euler(180, 0, 0).ToQuaternion();

            this.position = position * -1 / 1000;
            this.rotation = AzureToUnity(azureRotation);

            switch (joint) {
                case JointId.Pelvis:
                    this.name = "SpineBase";
                    break;
                case JointId.SpineNavel:
                    this.name = "SpineMid";
                    break;
                case JointId.SpineChest:
                    this.name = "SpineShoulder";
                    break;
                case JointId.Neck:
                    this.name = "Neck";
                    break;
                case JointId.ShoulderLeft:
                    this.name = "ShoulderLeft";
                    this.rotation *= armLeftRotation;
                    break;
                case JointId.ElbowLeft:
                    this.name = "ElbowLeft";
                    this.rotation *= armLeftRotation;
                    break;
                case JointId.WristLeft:
                    this.name = "WristLeft";
                    this.rotation *= armLeftRotation;
                    break;
                case JointId.HandLeft:
                    this.name = "HandLeft";
                    this.rotation *= armLeftRotation;
                    break;
                case JointId.ShoulderRight:
                    this.name = "ShoulderRight";
                    this.rotation *= armRightRotation;
                    break;
                case JointId.ElbowRight:
                    this.name = "ElbowRight";
                    this.rotation *= armRightRotation;
                    break;
                case JointId.WristRight:
                    this.name = "WristRight";
                    this.rotation *= armRightRotation;
                    break;
                case JointId.HandRight:
                    this.name = "HandRight";
                    this.rotation *= armRightRotation;
                    break;
                case JointId.HipLeft:
                    this.name = "HipLeft";
                    break;
                case JointId.KneeLeft:
                    this.name = "KneeLeft";
                    break;
                case JointId.AnkleLeft:
                    this.name = "AnkleLeft";
                    break;
                case JointId.HipRight:
                    this.name = "HipRight";
                    this.rotation *= legLeftRotation;
                    break;
                case JointId.KneeRight:
                    this.name = "KneeRight";
                    this.rotation *= legLeftRotation;
                    break;
                case JointId.AnkleRight:
                    this.name = "AnkleRight";
                    this.rotation *= legLeftRotation;
                    break;
                case JointId.Head:
                    this.name = "Head";
                    break;
                //case JointId.ClavicleLeft:
                //    break;
                //case JointId.HandTipLeft:
                //    break;
                //case JointId.ThumbLeft:
                //    break;
                //case JointId.ClavicleRight:
                //    break;
                //case JointId.HandTipRight:
                //    break;
                //case JointId.ThumbRight:
                //    break;
                //case JointId.FootLeft:
                //    break;
                //case JointId.FootRight:
                //    break;
                //case JointId.Nose:
                //    break;
                //case JointId.EyeLeft:
                //    break;
                //case JointId.EarLeft:
                //    break;
                //case JointId.EyeRight:
                //    break;
                //case JointId.EarRight:
                //    break;
                default:
                    throw new Exception("Joint not implemented");
            }
        }
    }
}
