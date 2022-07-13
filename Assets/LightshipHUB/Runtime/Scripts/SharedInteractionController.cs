using UnityEngine;
using UnityEngine.UI;

using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.HitTest;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.Extensions;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Input.Legacy;

namespace Niantic.ARDK.Templates 
{
    public class SharedInteractionController : MonoBehaviour 
    {
        [HideInInspector]
        public SharedSession SharedSession;
        public float TriggerDistance = 1.5f;

        Camera _camera;

        [SerializeField] float _winTime = 20;
        float _stayTime = 0;

        [SerializeField] Text _winText, _loseText;
        [SerializeField] LayerMask _layerMask;
        private void Start()
        {
            _camera = Camera.main;
            _winText.gameObject.SetActive(false);
            _loseText.gameObject.SetActive(false);
        }

        private void Update() 
        {
            if (IsInZone())
            {
                _stayTime += Time.deltaTime;

                if(_stayTime > _winTime)
                {
                    //win

                    Debug.Log("WINNER");
                    _winText.gameObject.SetActive(true);
                    SharedSession.SharedObjectHolder.gameObject.SetActive(false);
                    enabled = false;

                    return;
                }
            }

            if (PlatformAgnosticInput.touchCount <= 0) return;
            
            var touch = PlatformAgnosticInput.GetTouch(0);
            if (touch.phase == TouchPhase.Began) 
            {
                TouchBegan(touch);
            }
            
        }

        private bool IsInZone()
        {
            return SharedSession.SharedObjectHolder.gameObject.transform.GetChild(0).GetComponent<Collider>().bounds.Contains(_camera.transform.position);
        }

        private void TouchBegan(Touch touch) 
        {
            Debug.Log("touch1");
            var currentFrame = SharedSession._arNetworking.ARSession.CurrentFrame;
            if (currentFrame == null) return;
            if (SharedSession._camera == null) return;
            Debug.Log("touch2");
            var worldRay = SharedSession._camera.ScreenPointToRay(touch.position);
            RaycastHit hit;

            if (Physics.Raycast(worldRay, out hit, 1000f)) 
            {
                if (hit.transform.IsChildOf(SharedSession.SharedObjectHolder.gameObject.transform)) 
                {
            Debug.Log("touch3");
                    if (SharedSession._isHost) SharedSession.SharedObjectHolder.ObjectInteraction.AnimateObjectTap();
                    else SharedSession._messagingManager.AskHostToAnimateObjectTap(SharedSession._host);
                } 
            } 
            else 
            {
            Debug.Log("touch4");
                var hitTestResults = currentFrame.HitTest (
                    SharedSession._camera.pixelWidth, 
                    SharedSession._camera.pixelHeight, 
                    touch.position, 
                    ARHitTestResultType.All
                );

                if (hitTestResults.Count <= 0) 
                {
                    var ray = SharedSession._camera.ScreenPointToRay (touch.position);
                    if(Physics.Raycast(ray, out RaycastHit hitValue, 10f, _layerMask))
                    {
                                Debug.Log("touch5");
                        if (SharedSession._isHost)
                        {
                            if (!IsInZone() || !SharedSession.SharedObjectHolder.gameObject.activeSelf) return;
                            if (!SharedSession.SharedObjectHolder.gameObject.activeSelf && SharedSession._isStable)
                            {
                                Debug.Log("touch6");
                                SharedSession.SharedObjectHolder.gameObject.SetActive(true);
                            }
                            SharedSession.SharedObjectHolder.MoveObject(hitValue.point);
                            Debug.Log("touch7");
                        }
                        else
                        {
                            if (!IsInZone()) return;
                            SharedSession._messagingManager.AskHostToMoveObject(SharedSession._host, hitValue.point);
                        }
                    }
                    return;
                }

                var position = hitTestResults[0].WorldTransform.ToPosition();

            Debug.Log("touch8");
                if (SharedSession._isHost) 
                {
                    if (!IsInZone() && SharedSession.SharedObjectHolder.gameObject.activeSelf) return;

                    if (!SharedSession.SharedObjectHolder.gameObject.activeSelf && SharedSession._isStable) 
                    {
            Debug.Log("touch9");
                        SharedSession.SharedObjectHolder.gameObject.SetActive(true);
                    }
                    SharedSession.SharedObjectHolder.MoveObject(position);
            Debug.Log("touch10");
                }
                else 
                {
                    if (!IsInZone()) return;

                    SharedSession._messagingManager.AskHostToMoveObject(SharedSession._host, position);
                }
            }
        }
    }
}
