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
   
    private RuntimeImageHandler _runtimeImageHandler;
    
    private bool _isSession = false;

    
    private Dictionary<string, Vector3> initialPositions = new Dictionary<string, Vector3>();
    private Dictionary<string, float> lostTrackingTimes = new Dictionary<string, float>();
    
    public float positionThreshold = 0.1f;
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
    }

    private void OnDestroy()
    {
        _trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImage);
    }
    

    private void Init()
    {
        if (_dicCustomCard == null || _dicCustomCard.Count == 0)
            return;
        Debug.Log("Init Start!");
        StartCoroutine(InitCo());
    }

    private IEnumerator InitCo()
    {
       // yield return new WaitUntil(() => _isSession);
        _runtimeImageHandler = new RuntimeImageHandler(_dicCustomCard);
        yield return _runtimeImageHandler.ImageJobInitCo(_trackedImageManager.referenceLibrary,_customImageLibrarySo.GetImages(),
        (onComplete) =>
        {
            if (onComplete)
            {
                Debug.Log("Image Job Complete!");
                //화면 나타내기.
            }
        });
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
    
    private void UpdateImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;
        if (_cardDic.Keys.Contains(imageName) == false)
            return;
        GameObject childObject  = _cardDic[imageName];
       
        // 기존 위치의 이동 평균 필터 적용
        Vector3 previousPosition = initialPositions[imageName];
        Vector3 currentPosition = trackedImage.transform.position;
        if (trackedImage.trackingState == TrackingState.Tracking)
        {
            childObject.transform.localRotation = Quaternion.Euler(new Vector3(0,180,0));
            if (childObject.activeSelf == false)
            {
                childObject.SetActive(true);
                Debug.Log($"{childObject.name} has been setactive true");
            }
           
            if (Vector3.Distance(previousPosition, currentPosition) > positionThreshold)
            {
                childObject.transform.position = currentPosition;
            }
            
            // 트랙킹이 정상일 때는 기록된 트랙킹 손실 시간 초기화
            if (lostTrackingTimes.ContainsKey(imageName))
                lostTrackingTimes.Remove(imageName);
        }
        else
        {
            //childObject.SetActive(false);
            // 트랙킹 상태가 아니라면 시간을 기록하거나 업데이트
            if (!lostTrackingTimes.ContainsKey(imageName))
            {
                lostTrackingTimes[imageName] = Time.time;  // 트랙킹을 잃은 시간 기록
            }
            else
            {
                // 트랙킹을 잃은 시간이 2초 이상 지났다면 오브젝트 비활성화
                if (Time.time - lostTrackingTimes[imageName] >= 1f)
                {
                    childObject.SetActive(false);
                    Debug.Log($"{childObject.name} has been set inactive after 2 seconds of lost tracking.");
                }
            }
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
        o.transform.position = img.transform.position;
        o.transform.rotation =  Quaternion.Euler(go.offsetRotation);

     //  o.transform.SetParent(null);
        _cardDic[img.referenceImage.name] = o;
    }
}
