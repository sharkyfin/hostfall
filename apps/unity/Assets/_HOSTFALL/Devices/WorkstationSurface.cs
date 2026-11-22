using System;
using Hostfall.Simulation;
using UnityEngine;

namespace Hostfall.Devices
{
    public sealed class WorkstationSurface : MonoBehaviour
    {
        [SerializeField] private Transform topLeft,topRight,bottomLeft,bottomRight;
        [SerializeField] private DeviceId device;
        [SerializeField] private TerminalOsFamily os;
        private Renderer[] labelRenderers;private bool nativeOverlay;
        public DeviceId Device=>device;
        public TerminalOsFamily Os=>os;
        public bool Valid=>topLeft!=null&&topRight!=null&&bottomLeft!=null&&bottomRight!=null;
        public Vector3 TopLeft=>topLeft.position;
        public Vector3 TopRight=>topRight.position;
        public Vector3 BottomLeft=>bottomLeft.position;
        public Vector3 BottomRight=>bottomRight.position;
        public Vector3 Centre=>(TopLeft+BottomRight)*.5f;
        public Vector3 Normal=>-Vector3.Cross(TopRight-TopLeft,TopLeft-BottomLeft).normalized;
        public Vector3 Up=>(TopLeft-BottomLeft).normalized;
        public void Configure(DeviceId id,TerminalOsFamily family,Transform tl,Transform tr,Transform bl,Transform br)
        {device=id;os=family;topLeft=tl;topRight=tr;bottomLeft=bl;bottomRight=br;}
        public void FocusPose(float aspect,out Vector3 position,out Quaternion rotation,out float fov)
        {
            fov=35;
            float h=Vector3.Distance(TopLeft,BottomLeft),w=Vector3.Distance(TopLeft,TopRight);
            float d=Mathf.Max(h/.72f,w/(Mathf.Max(.5f,aspect)*.86f))*.5f/Mathf.Tan(fov*Mathf.Deg2Rad*.5f);
            position=Centre+Normal*d;rotation=Quaternion.LookRotation(-Normal,Up);
        }
        public void SetNativeOverlay(bool visible)
        {if(nativeOverlay==visible)return;nativeOverlay=visible;labelRenderers??=Array.ConvertAll(GetComponentsInChildren<TextMesh>(true),label=>label.GetComponent<Renderer>());foreach(var renderer in labelRenderers)renderer.enabled=!visible;}
    }
}
