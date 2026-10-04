using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class FruitNutritionApp
{
    private void AddHomeHero()
    {
        var card=AddCard("Daily scan summary",178,SoftGreenColor);
        int today=LocalRepository.GetHistory().Count(r=>System.DateTime.TryParse(r.scannedAt,out var date)&&date.Date==System.DateTime.Now.Date);
        AddCardText(card.transform,"Daily fruit scans",13,FontStyle.Bold,PrimaryColor,new Vector2(.07f,.72f),new Vector2(.72f,.93f),TextAnchor.MiddleLeft);
        AddCardText(card.transform,$"{today} scans",30,FontStyle.Bold,InkColor,new Vector2(.07f,.42f),new Vector2(.76f,.72f),TextAnchor.MiddleLeft);
        AddCardText(card.transform,"ผลไม้ที่คุณสแกนวันนี้",12,FontStyle.Normal,MutedColor,new Vector2(.07f,.28f),new Vector2(.8f,.44f),TextAnchor.MiddleLeft);
        var apple=FruitDatabase.Instance.Find("apple");var orange=FruitDatabase.Instance.Find("orange");
        if(apple!=null)AddFruitArt(card.transform,apple,new Vector2(.74f,.32f),62);
        if(orange!=null)AddFruitArt(card.transform,orange,new Vector2(.89f,.27f),48);
        AddButton("Scan Fruit",OpenScan,primary:true);
    }
    private GameObject AddCard(string name,float height,Color? color=null)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(LayoutElement));
        go.transform.SetParent(scrollContent,false);
        var image=go.GetComponent<Image>();SetRoundedSurface(image,color??SurfaceColor);image.raycastTarget=false;
        go.GetComponent<LayoutElement>().preferredHeight=height;
        if((color??SurfaceColor).a>0)
        {var shadow=go.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.055f);shadow.effectDistance=new Vector2(0,-3);}
        return go;
    }
    private Button AddButton(string label,UnityEngine.Events.UnityAction action,float? x=null,float y=-1,bool primary=false,bool accent=false)
    {
        var go=AddCard(label,54,SurfaceColor);
        var image=go.GetComponent<Image>();image.raycastTarget=true;
        var button=go.AddComponent<Button>();button.targetGraphic=image;
        var colors=button.colors;colors.normalColor=primary?PrimaryColor:accent?SoftGreenColor:SurfaceColor;
        colors.selectedColor=colors.normalColor;colors.highlightedColor=primary?DarkGreenColor:SoftGreenColor;colors.pressedColor=colors.highlightedColor;colors.fadeDuration=0;button.colors=colors;
        button.onClick.AddListener(action);
        AddCardText(go.transform,label,15,FontStyle.Bold,primary?SurfaceColor:InkColor,new Vector2(.04f,.02f),new Vector2(.96f,.98f),TextAnchor.MiddleCenter);
        return button;
    }
    private void AddFruitRow(FruitData fruit,string subtitle,UnityEngine.Events.UnityAction action)
    {
        var button=AddButton("",action);button.GetComponent<LayoutElement>().preferredHeight=88;
        var hasArt=AddFruitArt(button.transform,fruit,new Vector2(.13f,.5f),48);
        var textLeft=hasArt ? 0.26f : 0.07f;
        AddCardText(button.transform,fruit.englishName,16,FontStyle.Bold,InkColor,new Vector2(textLeft,.5f),new Vector2(.8f,.88f),TextAnchor.MiddleLeft);
        AddCardText(button.transform,subtitle,10,FontStyle.Normal,MutedColor,new Vector2(textLeft,.14f),new Vector2(.81f,.52f),TextAnchor.MiddleLeft);
        AddCardText(button.transform,fruit.healthScore.ToString(),17,FontStyle.Bold,PrimaryColor,new Vector2(.82f,.2f),new Vector2(.97f,.8f),TextAnchor.MiddleCenter);
    }
    private void AddHeading(string value)
    {
        var go=AddCard("Heading",30,Color.clear);
        AddCardText(go.transform,value,17,FontStyle.Bold,InkColor,Vector2.zero,Vector2.one,TextAnchor.MiddleLeft);
    }
    private void AddText(string value)
    {
        var go=AddCard("Information",90);
        AddCardText(go.transform,value,13,FontStyle.Normal,MutedColor,new Vector2(.06f,.1f),new Vector2(.94f,.9f),TextAnchor.MiddleLeft);
    }
    private void AddRule(Transform parent,float y)
    {
        var rect=MakeRect("Divider",parent);rect.anchorMin=new Vector2(.04f,y);rect.anchorMax=new Vector2(.96f,y);rect.sizeDelta=new Vector2(0,1);
        var image=rect.gameObject.AddComponent<Image>();image.color=BorderColor;image.raycastTarget=false;
    }
    private void AddCardText(Transform parent,string value,int size,FontStyle style,Color color,Vector2 min,Vector2 max,TextAnchor anchor)
    {
        // The bundled runtime font uses fallback Thai glyphs; synthetic bold causes
        // overlapping marks on some devices, so keep Thai labels at normal weight.
        var weight=value.Any(c=>c>='\u0E00'&&c<='\u0E7F')?FontStyle.Normal:style;
        var text=MakeText("Label",parent,size,weight,min,max,anchor);text.text=value;text.color=color;text.raycastTarget=false;
    }
    private bool AddFruitArt(Transform parent,FruitData fruit,Vector2 center,float size)
    {
        var texture=ResolveFruitArt(fruit,size);
        if(texture==null)return false;
        var rect=MakeRect("Fruit artwork",parent);rect.anchorMin=rect.anchorMax=center;rect.sizeDelta=new Vector2(size,size);
        var image=rect.gameObject.AddComponent<Image>();image.sprite=texture;image.preserveAspect=true;image.raycastTarget=false;
        return true;
    }
    private static Sprite ResolveFruitArt(FruitData fruit,float size)
    {
        if(fruit==null||!fruitArtResourceById.TryGetValue(fruit.id,out var resourceName))
        {
            LogMissingFruitArt(fruit?.id);
            return null;
        }

        var sprite=LoadArt(resourceName);
        if(sprite==null)LogMissingFruitArt(fruit.id);
        return sprite;
    }
    private static void LogMissingFruitArt(string fruitId)
    {
        if(string.IsNullOrEmpty(fruitId)||!missingFruitArtLogged.Add(fruitId))return;
        Debug.Log("FRUIT_ART_UNAVAILABLE id="+fruitId+"; no 2D artwork asset is mapped in Resources/FruitArt.");
    }
    private static Sprite LoadArt(string name)
    {
        if(artSprites.TryGetValue(name,out var sprite))return sprite;
        sprite=Resources.Load<Sprite>("FruitArt/"+name);if(sprite==null)return null;
        artSprites[name]=sprite;return sprite;
    }
    private void EnableScrollableButtons()
    {
        var view=MakeRect("Scrollable viewport",body);view.anchorMin=Vector2.zero;view.anchorMax=Vector2.one;view.offsetMin=view.offsetMax=Vector2.zero;
        view.gameObject.AddComponent<RectMask2D>();
        // A transparent raycast surface allows dragging between cards.
        var image=view.gameObject.AddComponent<Image>();image.color=Color.clear;
        var content=MakeRect("Content",view);content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;
        var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(8,8,8,20);layout.spacing=16;
        layout.childControlWidth=layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var scroll=view.gameObject.AddComponent<ScrollRect>();scroll.viewport=view;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=24;
        scrollContent=content;
    }
    private void BuildNavigation(Transform root)
    {
        navigationBar=MakeRect("Bottom navigation",root);navigationBar.anchorMin=Vector2.zero;navigationBar.anchorMax=new Vector2(1,.09f);navigationBar.offsetMin=navigationBar.offsetMax=Vector2.zero;
        navigationBar.gameObject.AddComponent<Image>().color=SurfaceColor;
        string[] labels={"home","scan","history","heart","search"};
        string[] assets={"home","scan","history","favorite","search"};
        for(int i=0;i<5;i++)
        {
            int index=i;var hit=ActionAt(navigationBar,"",()=>Navigate(index),new Vector2(i*.2f,0),new Vector2((i+1)*.2f,1));
            var icon=MakeRect("Icon",hit.transform);icon.anchorMin=icon.anchorMax=new Vector2(.5f,.6f);icon.sizeDelta=new Vector2(25,25);
            var image=icon.gameObject.AddComponent<Image>();image.sprite=LoadArt("nav_"+assets[i]);image.preserveAspect=true;image.raycastTarget=false;navigationIcons.Add(image);
            var label=MakeText(labels[i],hit.transform,9,FontStyle.Normal,new Vector2(0,.05f),new Vector2(1,.3f),TextAnchor.MiddleCenter);
            label.raycastTarget=false;navigationLabels.Add(label);
        }
    }
    private void SetNavigationVisible(bool visible)
    {
        navigationBar.gameObject.SetActive(visible);
        int active=page==Page.Home?0:page==Page.History?2:page==Page.Favorites?3:page==Page.Fruits?4:-1;
        for(int i=0;i<5;i++){navigationIcons[i].color=i==active?PrimaryColor:MutedColor;navigationLabels[i].color=i==active?PrimaryColor:MutedColor;}
    }
    private void Navigate(int tab)
    {switch(tab){case 0:ShowHome();break;case 1:OpenScan();break;case 2:ShowList(Page.History);break;case 3:ShowList(Page.Favorites);break;case 4:ShowList(Page.Fruits);break;}}
    private void SetRoundedSurface(Image image,Color color)
    {image.sprite=GetRoundedSprite();image.type=Image.Type.Sliced;image.color=color;image.pixelsPerUnitMultiplier=1.2f;}
    private static Sprite GetRoundedSprite()
    {
        if(roundedSprite!=null)return roundedSprite;
        const int size=96,radius=24;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {float cx=Mathf.Clamp(x,radius,size-radius-1),cy=Mathf.Clamp(y,radius,size-radius-1);pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(radius+.5f-Vector2.Distance(new Vector2(x,y),new Vector2(cx,cy))));}
        texture.SetPixels(pixels);texture.Apply();
        return roundedSprite=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(radius,radius,radius,radius));
    }
    private static Sprite CreateHealthRingSprite(float progress)
    {
        string key="health_"+Mathf.RoundToInt(progress*100);if(artSprites.TryGetValue(key,out var cached))return cached;
        const int size=128;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float dx=x-63.5f,dy=y-63.5f,d=Mathf.Sqrt(dx*dx+dy*dy);
            float angle=Mathf.Repeat(90-Mathf.Atan2(dy,dx)*Mathf.Rad2Deg,360);
            var color=angle<=Mathf.Clamp01(progress)*360?PrimaryColor:SoftGreenColor;
            color.a=Mathf.Clamp01(Mathf.Min(d-43,57-d)+.5f);pixels[y*size+x]=color;
        }
        texture.SetPixels(pixels);texture.Apply();var sprite=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100);artSprites[key]=sprite;return sprite;
    }
    private static RectTransform MakeRect(string name,Transform parent)
    {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return go.GetComponent<RectTransform>();}
    private static Text MakeText(string name,Transform parent,int size,FontStyle style,Vector2 min,Vector2 max,TextAnchor align)
    {
        var rect=MakeRect(name,parent);rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var text=rect.gameObject.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text=name;text.fontSize=size;text.fontStyle=style;text.color=InkColor;text.alignment=align;
        text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;text.raycastTarget=false;return text;
    }
}
