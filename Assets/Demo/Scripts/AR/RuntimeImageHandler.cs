using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

[Serializable]
public class CustomData
{
    public string id;
    public Texture2D texture;
    public GameObject trackingPrefab;
    public Vector3 offsetRotation;
}

public class RuntimeImageHandler
{
    private Dictionary<string, CustomData> _dicCustomDatas;
    private List<AddReferenceImageJobState> _jobStates;
    private bool _isSession = false;
    private float width = 1;
    
    public RuntimeImageHandler( Dictionary<string, CustomData> dic)
    {
        width = 1;
        _jobStates = new List<AddReferenceImageJobState>();
        _dicCustomDatas = dic;
        ARSession.stateChanged -= ARSessionStateChanged;
        ARSession.stateChanged += ARSessionStateChanged;
    }
    
    private void ARSessionStateChanged(ARSessionStateChangedEventArgs stateEventArgs)
    {
        if (stateEventArgs.state is ARSessionState.SessionInitializing or ARSessionState.SessionTracking)
            _isSession = true;
    }

    public IEnumerator ImageJobInitCo(IReferenceImageLibrary Ilib,List<Texture2D> texs, Action<string,bool> onComplete)
    {
        yield return new WaitUntil(() => _isSession);
        IReferenceImageLibrary lib = Ilib;

        if (lib is MutableRuntimeReferenceImageLibrary mLib)
        {
            foreach (KeyValuePair<string, CustomData> data in _dicCustomDatas)
            {
                if (data.Value.texture.isReadable == false)
                {
                    Debug.LogError($"Error : {data.Key} is not readable!");
                    continue;
                }

                Texture2D readableTexture = MakeTextureReadable(data.Value.texture, TextureFormat.RGBA32);
                
                _jobStates.Add(mLib.ScheduleAddImageWithValidationJob(readableTexture,data.Key,width));
                yield return null;
            }

            yield return CheckJobCompletionCo(texs, (result,isComplete) =>
            {
                if (isComplete)
                {
                    onComplete?.Invoke(result,true);
                }
                else
                {
                    onComplete?.Invoke(result,false);
                }
            });
        }
    }

    private IEnumerator CheckJobCompletionCo(List<Texture2D> imgs, Action<string,bool> onComplete)
    {
        for (int i = 0; i < _jobStates.Count; i++)
        {
            var jobState = _jobStates[i];
            var image = imgs[i];

            while (!jobState.jobHandle.IsCompleted)
            {
                yield return null;
            }
            jobState.jobHandle.Complete();
            if (jobState.status == AddReferenceImageJobStatus.Success)
            {
                Debug.Log($"Successfully added image: {image.name}");
                onComplete?.Invoke(image.name,true);
            }
            else
            {
                Debug.LogError($"Failed to add image: {image.name} - Status: {jobState.status}");
                onComplete?.Invoke(image.name,false);
            }
        }
    } 
 
    private Texture2D MakeTextureReadable(Texture2D originalTexture, TextureFormat format)
    {
        string name = originalTexture.name;
        Texture2D readableTexture = new Texture2D(originalTexture.width, originalTexture.height, format, false);
        RenderTexture rt = RenderTexture.GetTemporary(originalTexture.width, originalTexture.height);
        Graphics.Blit(originalTexture, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        readableTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        readableTexture.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        readableTexture.name = name;
        return readableTexture;
    }

}
