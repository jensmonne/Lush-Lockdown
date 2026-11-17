using NUnit.Framework;
using Unity.GraphToolkit.Editor;
using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [SerializeField] private float CurrentAngle;
    [SerializeField] private float TargetSnapAngel;
    [SerializeField] private List<PortDirection> RingPorts;
    [SerializeField] private bool isBeingHeld;
    public void OnGrabStart() 
    {

    }
    public void OnGrabEnd() 
    {

    }
    public void RotateRing(float deltaAngel) 
    {

    }
    public void SnapToNearest90() 
    {

    }
    public void GetRotatedPorts(List<PortDirection>)   
    {

    }
}
