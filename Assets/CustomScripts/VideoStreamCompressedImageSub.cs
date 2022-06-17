using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Microsoft;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.UI;
using System.Threading;

using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    [RequireComponent(typeof(RosConnector))]
    public class VideoStreamCompressedImageSub : UnitySubscriber<MessageTypes.Sensor.CompressedImage>
    {
              
        public MeshRenderer meshRenderer;
        //public GameObject MeshRendererGameObject;
        
        private Texture2D texture2D;
        private byte[] imageData;
        private bool isMessageReceived;
        private bool isMeshRendererEnabled;

        private int set_toggle;

        protected override void Start()
        {
            base.Start();
            texture2D = new Texture2D(1, 1);
            meshRenderer.material = new Material(Shader.Find("Standard"));
            isMeshRendererEnabled = false;
            set_toggle = 0;
        }

        public void MakeVideoStreamActive(GameObject MeshRenderGameObject)
        {
            if (set_toggle == 0)
            {
                isMeshRendererEnabled = true;
                MeshRenderGameObject.SetActive(isMeshRendererEnabled);
                set_toggle = 1;
            } else
            {
                isMeshRendererEnabled = false;
                MeshRenderGameObject.SetActive(isMeshRendererEnabled);
                set_toggle = 0;
            }
        }

        private void Update()
        {
            if (isMessageReceived && isMeshRendererEnabled)
                ProcessMessage();
        }

        protected override void ReceiveMessage(MessageTypes.Sensor.CompressedImage compressedImage)
        {
            imageData = compressedImage.data;
            isMessageReceived = true;
        }

        private void ProcessMessage()
        {
            texture2D.LoadImage(imageData);
            texture2D.Apply();
            meshRenderer.material.SetTexture("_MainTex", texture2D);
            isMessageReceived = false;
        }

    }
}

