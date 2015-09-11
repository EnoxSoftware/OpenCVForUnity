using UnityEngine;
using System.Collections;

namespace OpenCVForUnitySample
{
		public class ShowLicense : MonoBehaviour
		{

				// Use this for initialization
				void Start ()
				{
	
				}
	
				// Update is called once per frame
				void Update ()
				{
	
				}

				private Vector2 scrollViewVector = Vector2.zero;

				void OnGUI ()
				{
						float screenScale = 1.0f;
						if (Screen.width < Screen.height) {
								screenScale = Screen.width / 240.0f;
						} else {
								screenScale = Screen.height / 360.0f;
						}
						Matrix4x4 scaledMatrix = Matrix4x4.Scale (new Vector3 (screenScale, screenScale, screenScale));
						GUI.matrix = scaledMatrix;

						GUILayout.BeginVertical ();
						if (GUILayout.Button ("back")) {
								Application.LoadLevel ("OpenCVForUnitySample");
						}

						scrollViewVector = GUI.BeginScrollView (new Rect (10, 25, 225, 325), scrollViewVector, new Rect (0, 0, 205, 1100));

						GUI.Label (new Rect (0, 0, 205, 1100), "IMPORTANT: READ BEFORE DOWNLOADING, COPYING, INSTALLING OR USING.\n\nBy downloading, copying, installing or using the software you agree to this license.  If you do not agree to this license, do not download, install,  copy or use the software.\n\nLicense Agreement\nFor Open Source Computer Vision Library\n\nCopyright (C) 2000-2008, Intel Corporation, all rights reserved. \nCopyright (C) 2008-2011, Willow Garage Inc., all rights reserved. \nThird party copyrights are property of their respective owners.\nRedistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:\n\n* Redistributions of source code must retain the above copyright notice,     this list of conditions and the following disclaimer.\n\n* Redistributions in binary form must reproduce the above copyright notice,     this list of conditions and the following disclaimer in the documentation     and/or other materials provided with the distribution.\n\n* The name of the copyright holders may not be used to endorse or promote products     derived from this software without specific prior written permission.\n\nThis software is provided by the copyright holders and contributors \"as is\" and any express or implied warranties, including, but not limited to, the implied warranties of merchantability and fitness for a particular purpose are disclaimed. In no event shall the Intel Corporation or contributors be liable for any direct, indirect, incidental, special, exemplary, or consequential damages (including, but not limited to, procurement of substitute goods or services; loss of use, data, or profits; or business interruption) however caused and on any theory of liability, whether in contract, strict liability,\nor tort (including negligence or otherwise) arising in any way out of the use of this software, even if advised of the possibility of such damage. ");

						GUI.EndScrollView ();

						GUILayout.EndVertical ();
				}
		}
}
