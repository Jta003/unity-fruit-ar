using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// Screen composition follows the supplied 390 x 844 SVG. Shared navigation,
// persistence, inference and AR lifecycle remain in FruitNutritionApp.
public sealed partial class FruitNutritionApp
{
    private readonly List<GameObject> pageDecorations = new List<GameObject>();

    private RectTransform Panel(string name, Transform parent, Color color, Vector2 min, Vector2 max)
    {
        var rect = MakeRect(name, parent);
        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = rect.gameObject.AddComponent<Image>();
        SetRoundedSurface(image, color); image.raycastTarget = false;
        return rect;
    }

    private Button ActionAt(Transform parent, string label, Action action, Vector2 min, Vector2 max, bool primary = false)
    {
        var rect = Panel(label, parent, SurfaceColor, min, max);
        var image = rect.GetComponent<Image>(); image.raycastTarget = true;
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = primary ? PrimaryColor : SurfaceColor;
        colors.highlightedColor = primary ? DarkGreenColor : SoftGreenColor;
        colors.selectedColor = colors.normalColor; colors.pressedColor = colors.highlightedColor;
        colors.fadeDuration = 0; button.colors = colors; button.onClick.AddListener(() => action());
        AddCardText(rect, label, 14, FontStyle.Bold, primary ? SurfaceColor : InkColor,
            new Vector2(.04f, .06f), new Vector2(.96f, .94f), TextAnchor.MiddleCenter);
        return button;
    }

    private void TopBack(Action action)
    {
        var button = ActionAt(safeArea, "‹", action, new Vector2(.045f, .922f), new Vector2(.145f, .973f));
        pageDecorations.Add(button.gameObject);
        headerLabel.rectTransform.anchorMin = new Vector2(.18f, .92f);
        headerLabel.alignment = TextAnchor.MiddleCenter;
    }

    private void StandardHeader(string title, string subtitle = "")
    {
        headerLabel.text = title; status.text = subtitle;
        headerLabel.rectTransform.anchorMin = new Vector2(.06f, .92f);
        headerLabel.alignment = TextAnchor.MiddleLeft;
    }

    private void ShowHome()
    {
        openArAfterCapture = false; ExitCameraMode(); page = Page.Home; ClearPage();
        StandardHeader("Fruit Nutrition AR", "สแกนผลไม้ แล้วดูคุณค่าทางโภชนาการทันที");
        SetNavigationVisible(true); EnableScrollableButtons();
        AddHomeHero();
        var heading = AddCard("Recent Scan heading", 30, CanvasColor);
        AddCardText(heading.transform, "Recent Scan", 17, FontStyle.Bold, InkColor, Vector2.zero, new Vector2(.7f,1), TextAnchor.MiddleLeft);
        ActionAt(heading.transform, "View all", () => ShowList(Page.History), new Vector2(.72f,0), Vector2.one);
        var recent = LocalRepository.GetHistory().Select(r => FruitDatabase.Instance.Find(r.fruitId)).Where(f => f != null).GroupBy(f => f.id).Select(g => g.First()).Take(4).ToArray();
        if (recent.Length == 0) AddText("ยังไม่มีประวัติการสแกน\nแตะ Scan Fruit เพื่อเริ่มต้น");
        else
        {
            var row = AddCard("Recent fruit cards", 126, CanvasColor);
            for (int i = 0; i < recent.Length; i++)
            {
                var fruit = recent[i];
                var tile = ActionAt(row.transform, "", () => ShowDetail(fruit), new Vector2(i*.25f,0), new Vector2(i*.25f+.23f,1));
                var hasArt=AddFruitArt(tile.transform, fruit, new Vector2(.5f,.69f), 44);
                var nameMinY=hasArt ? 0.21f : 0.48f;
                var nameMaxY=hasArt ? 0.43f : 0.82f;
                AddCardText(tile.transform, fruit.englishName, 11, FontStyle.Bold, InkColor, new Vector2(.02f,nameMinY),new Vector2(.98f,nameMaxY),TextAnchor.MiddleCenter);
                AddCardText(tile.transform, $"{fruit.calories:0} kcal", 10, FontStyle.Normal, MutedColor, new Vector2(.02f,.04f),new Vector2(.98f,.23f),TextAnchor.MiddleCenter);
            }
        }
        var recommendation = AddCard("Smart recommendation", 98);
        AddCardText(recommendation.transform, "Smart recommendation", 15, FontStyle.Bold, InkColor, new Vector2(.08f,.55f),new Vector2(.94f,.9f),TextAnchor.MiddleLeft);
        AddCardText(recommendation.transform, "เปรียบเทียบพลังงาน น้ำตาล และใยอาหาร\nเพื่อเลือกผลไม้ให้เหมาะกับคุณ", 12, FontStyle.Normal, MutedColor, new Vector2(.08f,.1f),new Vector2(.94f,.55f),TextAnchor.MiddleLeft);
    }

    private void ShowDetail(FruitData fruit)
    {
        ExitCameraMode(); selected = fruit; page = Page.Detail; ClearPage();
        StandardHeader("Nutrition Detail"); status.gameObject.SetActive(false);
        SetNavigationVisible(false); TopBack(() => ShowList(Page.Fruits));
        backgroundPanel.color = CanvasColor;
        var hero = MakeRect("Detail fruit artwork", safeArea);
        hero.anchorMin = new Vector2(.2f,.77f); hero.anchorMax = new Vector2(.8f,.92f); hero.offsetMin = hero.offsetMax = Vector2.zero;
        var hasHeroArt=AddFruitArt(hero, fruit, new Vector2(.5f,.5f),112);
        var sheetTop=hasHeroArt ? .77f : .9f;
        if(hasHeroArt) pageDecorations.Add(hero.gameObject); else Destroy(hero.gameObject);
        var sheet = Panel("Detail sheet",safeArea,SurfaceColor,Vector2.zero,new Vector2(1,sheetTop));
        sheet.SetSiblingIndex(body.GetSiblingIndex());pageDecorations.Add(sheet.gameObject);
        body.anchorMin = new Vector2(.04f,.115f); body.anchorMax = new Vector2(.96f,sheetTop);
        EnableScrollableButtons();
        var name = AddCard("Fruit identity", 64);
        AddCardText(name.transform, fruit.englishName, 26, FontStyle.Bold, InkColor,new Vector2(.02f,.4f),new Vector2(.79f,.98f),TextAnchor.MiddleLeft);
        AddCardText(name.transform, fruit.scientificName,12,FontStyle.Normal,MutedColor,new Vector2(.02f,.02f),new Vector2(.8f,.4f),TextAnchor.MiddleLeft);
        var favorite = ActionAt(name.transform, "", () => { LocalRepository.ToggleFavorite(fruit.id); ShowDetail(fruit); },new Vector2(.84f,.3f),new Vector2(.99f,.96f));
        var icon = MakeRect("Favorite",favorite.transform); icon.anchorMin = new Vector2(.2f,.2f); icon.anchorMax = new Vector2(.8f,.8f); icon.offsetMin = icon.offsetMax = Vector2.zero;
        var art = icon.gameObject.AddComponent<Image>(); art.sprite = LoadArt("nav_favorite"); art.preserveAspect=true; art.raycastTarget=false;
        art.color = LocalRepository.IsFavorite(fruit.id) ? PrimaryColor : MutedColor;
        ActionAt(name.transform,"AR",()=>OpenAR(fruit),new Vector2(.66f,.3f),new Vector2(.81f,.96f));
        AddCompactNutrition(fruit);
        var compare=ActionAt(safeArea,"Compare Fruits",BeginCompare,new Vector2(.08f,.035f),new Vector2(.92f,.103f),true);
        pageDecorations.Add(compare.gameObject);
    }

    private void AddCompactNutrition(FruitData fruit)
    {
        var score = AddCard("Health score",112,SoftGreenColor);
        var ring = MakeRect("Health ring",score.transform); ring.anchorMin=ring.anchorMax=new Vector2(.18f,.5f); ring.sizeDelta=new Vector2(74,74);
        ring.gameObject.AddComponent<Image>().sprite=CreateHealthRingSprite(fruit.healthScore/100f);
        AddCardText(score.transform,$"{fruit.healthScore}/100",28,FontStyle.Bold,PrimaryColor,new Vector2(.35f,.48f),new Vector2(.96f,.89f),TextAnchor.MiddleLeft);
        AddCardText(score.transform,"Health Score",14,FontStyle.Bold,InkColor,new Vector2(.35f,.23f),new Vector2(.96f,.5f),TextAnchor.MiddleLeft);
        var metrics=AddCard("Nutrition highlights",90,Color.clear);
        string[] labels={"Energy","Fiber","Sugar"}; string[] values={$"{fruit.calories:0.#} kcal",$"{fruit.fiber:0.#} g",$"{fruit.sugar:0.#} g"};
        Color[] colors={AccentColor,PrimaryColor,DarkGreenColor};
        for(int i=0;i<3;i++)
        {
            var tile=Panel(labels[i],metrics.transform,SurfaceColor,new Vector2(i/3f,.02f),new Vector2((i+1)/3f-.035f,.98f));
            var dot=Panel("Metric color",tile,colors[i],new Vector2(.15f,.47f),new Vector2(.45f,.83f));
            AddCardText(tile,labels[i],10,FontStyle.Normal,MutedColor,new Vector2(.48f,.48f),new Vector2(.99f,.85f),TextAnchor.MiddleLeft);
            AddCardText(tile,values[i],17,FontStyle.Bold,InkColor,new Vector2(.12f,.08f),new Vector2(.98f,.45f),TextAnchor.MiddleLeft);
        }
        var facts=AddCard("Nutrition Facts",174);
        AddCardText(facts.transform,"Nutrition Facts",17,FontStyle.Bold,InkColor,new Vector2(.04f,.79f),new Vector2(.96f,1),TextAnchor.MiddleLeft);
        string[] names={"Carbohydrate","Protein","Vitamin C"};
        string[] amounts={$"{fruit.carbohydrate:0.#} g",$"{fruit.protein:0.#} g",$"{fruit.vitaminC:0.#} mg"};
        for(int i=0;i<3;i++)
        {
            float top=.76f-i*.21f; AddRule(facts.transform,top);
            AddCardText(facts.transform,names[i],12,FontStyle.Normal,MutedColor,new Vector2(.04f,top-.2f),new Vector2(.7f,top),TextAnchor.MiddleLeft);
            AddCardText(facts.transform,amounts[i],12,FontStyle.Bold,InkColor,new Vector2(.7f,top-.2f),new Vector2(.96f,top),TextAnchor.MiddleRight);
        }
        AddCardText(facts.transform,"หน่วยบริโภค: "+fruit.servingSize,10,FontStyle.Normal,MutedColor,new Vector2(.04f,0),new Vector2(.96f,.13f),TextAnchor.MiddleLeft);
    }

    private void ShowCompare()
    {
        ExitCameraMode(); page=Page.Compare; ClearPage(); StandardHeader("Compare Fruits"); SetNavigationVisible(true); TopBack(ShowHome); EnableScrollableButtons();
        if(compareA==null || compareB==null)
        {
            status.text=compareA==null ? "เลือกผลไม้ชนิดแรก" : "เลือกอีกหนึ่งชนิดเพื่อเทียบกับ "+compareA.englishName;
            if(compareA!=null) AddFruitRow(compareA,"ผลไม้ที่เลือกไว้",()=>{compareA=null;ShowCompare();});
            foreach(var item in FruitDatabase.Instance.All.Where(f=>compareA==null || f.id!=compareA.id))
                AddFruitRow(item,"แตะเพื่อเลือก",()=>{if(compareA==null)compareA=item;else compareB=item;ShowCompare();});
            return;
        }
        status.text="เปรียบเทียบตามหน่วยบริโภคของแต่ละผลไม้";
        var pair=AddCard("Compared fruits",174,Color.clear);
        for(int i=0;i<2;i++)
        {
            int side=i; var fruit=i==0?compareA:compareB;
            var tile=ActionAt(pair.transform,"",()=>{if(side==0){compareA=null;compareB=null;}else compareB=null;ShowCompare();},new Vector2(i*.54f,0),new Vector2(i*.54f+.46f,1));
            var hasArt=AddFruitArt(tile.transform,fruit,new Vector2(.5f,.66f),78);
            var nameMinY=hasArt ? 0.2f : 0.5f;
            var nameMaxY=hasArt ? 0.4f : 0.8f;
            AddCardText(tile.transform,fruit.englishName,19,FontStyle.Bold,InkColor,new Vector2(.03f,nameMinY),new Vector2(.97f,nameMaxY),TextAnchor.MiddleCenter);
            AddCardText(tile.transform,$"{fruit.calories:0.#} kcal",13,FontStyle.Normal,i==0?PrimaryColor:AccentColor,new Vector2(.03f,.04f),new Vector2(.97f,.2f),TextAnchor.MiddleCenter);
        }
        var card=AddCard("Nutrition comparison",270);
        AddCardText(card.transform,"Nutrition comparison",17,FontStyle.Bold,InkColor,new Vector2(.07f,.84f),new Vector2(.96f,.98f),TextAnchor.MiddleLeft);
        string[] nutrients={"Energy","Sugar","Fiber","Vitamin C"};
        float[] a={compareA.calories,compareA.sugar,compareA.fiber,compareA.vitaminC};
        float[] b={compareB.calories,compareB.sugar,compareB.fiber,compareB.vitaminC};
        for(int i=0;i<4;i++)
        {
            float y=.68f-i*.18f;
            AddCardText(card.transform,nutrients[i],12,FontStyle.Normal,MutedColor,new Vector2(.07f,y-.03f),new Vector2(.34f,y+.12f),TextAnchor.MiddleLeft);
            AddCardText(card.transform,$"{a[i]:0.#}",13,FontStyle.Bold,InkColor,new Vector2(.33f,y-.03f),new Vector2(.44f,y+.12f),TextAnchor.MiddleCenter);
            AddCardText(card.transform,$"{b[i]:0.#}",13,FontStyle.Bold,InkColor,new Vector2(.86f,y-.03f),new Vector2(.98f,y+.12f),TextAnchor.MiddleCenter);
            var track=Panel("Comparison bar",card.transform,SoftGreenColor,new Vector2(.46f,y+.035f),new Vector2(.85f,y+.065f));
            Panel("Value",track,i<2?PrimaryColor:AccentColor,Vector2.zero,new Vector2(a[i]+b[i]>0?a[i]/(a[i]+b[i]):0,1));
        }
        var insight=AddCard("Comparison insight",90,SoftGreenColor);
        AddCardText(insight.transform,"Nutrition insight",14,FontStyle.Bold,PrimaryColor,new Vector2(.07f,.56f),new Vector2(.96f,.94f),TextAnchor.MiddleLeft);
        var lower=compareA.sugar<=compareB.sugar?compareA:compareB;
        AddCardText(insight.transform,$"{lower.englishName} มีน้ำตาลน้อยกว่าหรือเท่ากัน\nตรวจหน่วยบริโภคในหน้ารายละเอียดก่อนเลือก",12,FontStyle.Normal,MutedColor,new Vector2(.07f,.07f),new Vector2(.96f,.57f),TextAnchor.MiddleLeft);
    }
}
