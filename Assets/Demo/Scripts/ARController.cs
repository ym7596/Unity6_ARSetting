using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARController : MonoBehaviour
{
    [SerializeField] private ARTrackedImageManager _trackedImageManager;

    [SerializeField] private GameObject Sinbi;
    [SerializeField] private GameObject Sujin;

    private Dictionary<string, GameObject> _cardDic;

    private void Start()
    {
        _cardDic = new Dictionary<string, GameObject>();
    }

    private void OnEnable()
    {
        _trackedImageManager.trackablesChanged.AddListener(OnTrackedImage);
    }

    private void OnDestroy()
    {
        _trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImage);
    }

    private void OnTrackedImage(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (ARTrackedImage newImage in eventArgs.added)
        {
            string name = newImage.referenceImage.name;
            Vector3 newScale = new Vector3(newImage.size.x, newImage.size.y, newImage.size.x);
            if (name == "sinbi")
            {
                SetInitTransform(newImage, Sinbi);
              
                Debug.Log("Sinbi!");
            }
            else if (name == "sujin")
            {
                SetInitTransform(newImage, Sujin);
             
                Debug.Log("Sujin!");
            }
        }

        foreach (ARTrackedImage updatedImage in eventArgs.updated)
        {
            string name = updatedImage.referenceImage.name;
            if (updatedImage.trackingState == TrackingState.Tracking)
            {
                if (name == "sinbi")
                {
                    _cardDic[name].transform.localPosition = Vector3.zero;
                    _cardDic[name].transform.localRotation =  Quaternion.Euler(new Vector3(0,180,0));
                }
                else if (name == "sujin")
                {
                    _cardDic[name].transform.localPosition = Vector3.zero;
                    _cardDic[name].transform.localRotation =  Quaternion.Euler(new Vector3(0,180,0));
                }
            }   
        }

        foreach (var removedImage in eventArgs.removed)
        {
           
        }
    }
    

    private void SetInitTransform(ARTrackedImage img,GameObject go)
    {
        Vector2 imageSize = img.size;
        
        GameObject o = Instantiate(go,img.transform);
        Vector3 newScale = new Vector3(imageSize.x, imageSize.y, imageSize.x);
        o.transform.localScale = newScale;
        o.transform.SetParent(img.transform);
        o.transform.localPosition = Vector3.zero;
        o.transform.localRotation =  Quaternion.Euler(new Vector3(0,180,0));


        _cardDic[img.referenceImage.name] = o;
    }
}
