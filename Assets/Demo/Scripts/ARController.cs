using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARController : MonoBehaviour
{
    [SerializeField] private ARTrackedImageManager _trackedImageManager;
    [SerializeField] private CustomImageLibrarySO _customImageLibrarySo;
    private Dictionary<string, GameObject> _cardDic;
    private Dictionary<string, CustomData> _dicCustomCard;
    private List<AddReferenceImageJobState> _jobStates = new List<AddReferenceImageJobState>();

    private bool _isSession = false;

    private void Awake()
    {
        if (_customImageLibrarySo)
        {
            _customImageLibrarySo.Init();
            _dicCustomCard = _customImageLibrarySo.dicTrackingData;
        }
    }

    private void Start()
    {
        _cardDic = new Dictionary<string, GameObject>();
        Init();
    }

    private void OnEnable()
    {
        _trackedImageManager.trackablesChanged.AddListener(OnTrackedImage);
        
        ARSession.stateChanged += ARSessionStateChanged;
    }

    private void OnDestroy()
    {
        _trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImage);
        
        ARSession.stateChanged -= ARSessionStateChanged;
    }
    
    private void ARSessionStateChanged(ARSessionStateChangedEventArgs stateEventArgs)
    {
        if (stateEventArgs.state is ARSessionState.SessionInitializing or ARSessionState.SessionTracking)
            _isSession = true;
    }

    private void Init()
    {
        if (_dicCustomCard == null || _dicCustomCard.Count == 0)
            return;
        StartCoroutine(InitCo());
    }

    private IEnumerator InitCo()
    {
        yield return new WaitUntil(() => _isSession);
        IReferenceImageLibrary lib = _trackedImageManager.referenceLibrary;

        if (lib is MutableRuntimeReferenceImageLibrary mLib)
        {
            foreach (KeyValuePair<string, CustomData> data in _dicCustomCard)
            {
                if (data.Value.texture.isReadable == false)
                {
                    Debug.LogError($"Error : {data.Key} is not readable!");
                    continue;
                }

                Texture2D readableTexture = MakeTextureReadable(data.Value.texture, TextureFormat.RGBA32);
                
                _jobStates.Add(mLib.ScheduleAddImageWithValidationJob(readableTexture,data.Key,_customImageLibrarySo.width));
                yield return null;
            }

            yield return CheckJobCompletion(_customImageLibrarySo.GetImages());
        }
    }
    
    private IEnumerator CheckJobCompletion(List<Texture2D> images)
    {
        for (int i = 0; i < _jobStates.Count; i++)
        {
            var jobState = _jobStates[i];
            var image = images[i];

            while (!jobState.jobHandle.IsCompleted)
            {
                yield return null;
            }
            jobState.jobHandle.Complete();
            if (jobState.status == AddReferenceImageJobStatus.Success)
            {
                Debug.Log($"Successfully added image: {image.name}");
            }
            else
            {
                Debug.LogError($"Failed to add image: {image.name} - Status: {jobState.status}");
            }
        }
    }
    
    private Texture2D MakeTextureReadable(Texture2D originalTexture, TextureFormat format)
    {
        Texture2D readableTexture = new Texture2D(originalTexture.width, originalTexture.height, format, false);
        RenderTexture rt = RenderTexture.GetTemporary(originalTexture.width, originalTexture.height);
        Graphics.Blit(originalTexture, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        readableTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        readableTexture.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return readableTexture;
    }

    private void OnTrackedImage(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (ARTrackedImage newImage in eventArgs.added)
        {
            AddImage(newImage);
        }

        foreach (ARTrackedImage updatedImage in eventArgs.updated)
        {
            UpdateImage(updatedImage);
        }

        foreach (var removedImage in eventArgs.removed)
        {
           RemoveImage(removedImage);
        }
    }

    private void AddImage(ARTrackedImage trackedImage)
    {
        string imgname = trackedImage.referenceImage.name;
        Debug.Log($"name : {imgname} has been reached");
        if (_dicCustomCard.TryGetValue(imgname, out CustomData trackData))
        {
            Debug.Log($"AddImage: Tracking {imgname} - Prefab: {trackData.trackingPrefab}");
            SetInitTransform(trackedImage, trackData);
        }
    }
    
    private void UpdateImage(ARTrackedImage tracked)
    {
        string imgName = tracked.referenceImage.name;

        if (tracked.trackingState == TrackingState.Tracking)
        {
            
        }
    }
    
    private void RemoveImage(KeyValuePair<TrackableId, ARTrackedImage> tracked)
    {
        
    }

    private void SetInitTransform(ARTrackedImage img,CustomData go)
    {
        Vector2 imageSize = img.size;
        if (_cardDic.Keys.Contains(img.referenceImage.name))
            return;
        
        GameObject o = Instantiate(go.trackingPrefab,img.transform);
        Vector3 newScale = new Vector3(imageSize.x, imageSize.y, imageSize.x);
        o.transform.localScale = newScale;
        o.transform.SetParent(img.transform);
        o.transform.localPosition = Vector3.zero;
        o.transform.localRotation =  Quaternion.Euler(go.offsetRotation);

     //  o.transform.SetParent(null);
        _cardDic[img.referenceImage.name] = o;
    }
}
