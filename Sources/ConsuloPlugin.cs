using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace MustBe.Consulo.Internal
{
	/// <summary>
	/// UnityEditor.Menu class is not exists in Unity 4.6 we need add some hack
	/// </summary>
	public class ConsuloPlugin
	{
		//private const int ourPort = 62242;
		private const int ourPort = 55333; // dev port
		private const String ourEditorPrefsKey = "UseConsuloAsExternalEditor";

#if UNITY_BEFORE_5

		[MenuItem("Edit/Use Consulo as External Editor", true)]
		static bool UseConsuloAsExternalEditorValidator()
		{
			return !UseConsulo();
		}

		[MenuItem("Edit/Disable Consulo as External Editor", true)]
		static bool DisableConsuloAsExternalEditorValidator()
		{
			return UseConsulo();
		}

		[MenuItem("Edit/Use Consulo as External Editor")]
		static void UseConsuloAsExternalEditor()
		{
			EditorPrefs.SetBool(ourEditorPrefsKey, true);
		}

		[MenuItem("Edit/Disable Consulo as External Editor")]
		static void DisableConsuloAsExternalEditor()
		{
			EditorPrefs.SetBool(ourEditorPrefsKey, false);
		}

#else

		[MenuItem("Edit/Use Consulo as External Editor", true)]
		static bool ValidateUncheckConsulo()
		{
			Menu.SetChecked("Edit/Use Consulo as External Editor", UseConsulo());
			return true;
		}

		[MenuItem("Edit/Use Consulo as External Editor")]
		static void UncheckConsulo()
		{
			var state = UseConsulo();
			Menu.SetChecked("Edit/Use Consulo as External Editor", !state);
			EditorPrefs.SetBool(ourEditorPrefsKey, !state);
		}
#endif

		static bool UseConsulo()
		{
			if(!EditorPrefs.HasKey(ourEditorPrefsKey))
			{
				EditorPrefs.SetBool(ourEditorPrefsKey, false);
				return false;
			}

			return EditorPrefs.GetBool(ourEditorPrefsKey);
		}

		[OnOpenAsset]
		static bool OnOpenedAssetCallback(int instanceID, int line)
		{
			if(!UseConsulo())
			{
				return false;
			}

			try
			{
				var projectPath = Path.GetDirectoryName(Application.dataPath);

				var selected = EditorUtility.InstanceIDToObject(instanceID);

				var filePath = projectPath + "/" + AssetDatabase.GetAssetPath(selected);

				var request = WebRequest.Create("http://localhost:" + ourPort + "/api/unityOpenFile");
				request.Timeout = 10000;
				request.Method = "POST";

				projectPath = projectPath.Replace('\\', '/');
				filePath = filePath.Replace('\\', '/');
				JSONClass jsonClass = new JSONClass();
				jsonClass.Add("projectPath", new JSONData(projectPath));
				jsonClass.Add("filePath", new JSONData(filePath));
				jsonClass.Add("editorPath", new JSONData(EditorApplication.applicationPath));
				jsonClass.Add("contentType", new JSONData(selected.GetType().ToString()));
				jsonClass.Add("line", new JSONData(line));

				var jsonClassToString = jsonClass.ToString();
				request.ContentType = "application/json; charset=utf-8";
				var bytes = Encoding.UTF8.GetBytes(jsonClassToString);
				request.ContentLength = bytes.Length;

				using (var stream = request.GetRequestStream())
				{
					stream.Write(bytes, 0, bytes.Length);
				}

				using (var requestGetResponse = request.GetResponse())
				{
					using (var responseStream = requestGetResponse.GetResponseStream())
					{
						using (var streamReader = new StreamReader(responseStream))
						{
							var result = JSON.Parse(streamReader.ReadToEnd());
							return result["success"].AsBool;
						}
					}
				}
			}
			catch(Exception e)
			{
				EditorUtility.DisplayDialog(PluginConstants.DIALOG_TITLE, "Consulo is not accessible at http://localhost:" + ourPort + ", message: " + e.Message, "OK");
			}
			return true;
		}
	}
}