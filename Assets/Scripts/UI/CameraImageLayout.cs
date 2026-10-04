using UnityEngine;
using UnityEngine.UI;

// Shared cover sizing for live and captured frames. Geometry is recalculated after
// layout so portrait buffers are never stretched to landscape rectangles.
[RequireComponent(typeof(RawImage))]
public sealed class CameraImageLayout : MonoBehaviour
{
    public int Rotation;
    public bool Mirrored;
    private void LateUpdate()
    {
        var image=GetComponent<RawImage>();
        if(image.texture==null || !(transform.parent is RectTransform viewport)) return;
        int angle=((Rotation%360)+360)%360;
        bool swap=angle==90 || angle==270;
        float width=swap?image.texture.height:image.texture.width;
        float height=swap?image.texture.width:image.texture.height;
        float scale=Mathf.Max(viewport.rect.width/width,viewport.rect.height/height);
        var rect=image.rectTransform;
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
        rect.anchoredPosition=Vector2.zero;
        rect.sizeDelta=new Vector2(image.texture.width*scale,image.texture.height*scale);
        rect.localEulerAngles=new Vector3(0,0,-angle);
        rect.localScale=new Vector3(1,Mirrored?-1:1,1);
    }
}
