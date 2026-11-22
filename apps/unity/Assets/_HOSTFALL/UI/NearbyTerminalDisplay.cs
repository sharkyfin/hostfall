using System;
using Hostfall.Devices;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
namespace Hostfall.UI
{
    // One bounded offscreen panel, shared by the approached workstation and its focused session.
    // The same DOM, texture, wrapping and physical quad survive entering/leaving the computer.
    public sealed class NearbyTerminalDisplay : IDisposable
    {
        public const int Width=1536,Height=856,TextureWidth=2048,TextureHeight=1142;
        private readonly GameObject owner,screen;
        private readonly UIDocument document;
        private readonly UIDocument sourceDocument;private readonly Image nativeGlass;
        private readonly PanelSettings settings;
        private readonly RenderTexture texture;
        private readonly Material material;
        private readonly Mesh mesh;private readonly MeshRenderer screenRenderer;
        private readonly WorkstationSurface[] surfaces;
        public WorkstationSurface Surface {get;private set;}
        public VisualElement View {get;}
        public RenderTexture Texture=>texture;
        public bool Interactive {get;set;}
        public NearbyTerminalDisplay(UIDocument source,VisualElement view,WorkstationSurface[] candidates)
        {
            surfaces=candidates;View=view;sourceDocument=source;
            texture=new RenderTexture(TextureWidth,TextureHeight,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){name="Nearby terminal / shared glass",filterMode=FilterMode.Bilinear};texture.Create();
            settings=UnityEngine.Object.Instantiate(source.panelSettings);settings.name="Physical terminal panel";settings.targetTexture=texture;settings.scaleMode=PanelScaleMode.ConstantPixelSize;settings.scale=TextureWidth/(float)Width;settings.clearColor=true;settings.colorClearValue=Color.clear;settings.SetScreenToPanelSpaceFunction(ScreenToPanel);
            owner=new GameObject("Physical terminal UI");owner.transform.SetParent(source.transform.parent,false);document=owner.AddComponent<UIDocument>();document.panelSettings=settings;document.sortingOrder=-100;
            var root=document.rootVisualElement;root.style.width=Width;root.style.height=Height;
            for(int i=0;i<source.rootVisualElement.styleSheets.count;i++)root.styleSheets.Add(source.rootVisualElement.styleSheets[i]);
            view.RemoveFromHierarchy();root.Add(view);view.AddToClassList("terminal-world");view.style.left=0;view.style.top=0;view.style.right=StyleKeyword.Auto;view.style.bottom=StyleKeyword.Auto;view.style.width=Width;view.style.height=Height;view.style.visibility=Visibility.Visible;view.style.display=DisplayStyle.None;
            nativeGlass=new Image{name="terminal-glass-image",image=texture,pickingMode=PickingMode.Ignore,scaleMode=ScaleMode.StretchToFill};nativeGlass.style.position=Position.Absolute;nativeGlass.style.display=DisplayStyle.None;source.rootVisualElement.Add(nativeGlass);
            screen=new GameObject("Live terminal glass");screen.transform.SetParent(source.transform,false);screen.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            mesh=new Mesh{name="Physical display quad"};screen.AddComponent<MeshFilter>().sharedMesh=mesh;
            material=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="Live terminal glass",mainTexture=texture};material.SetFloat("_Cull",0);
            screenRenderer=screen.AddComponent<MeshRenderer>();screenRenderer.sharedMaterial=material;screenRenderer.shadowCastingMode=ShadowCastingMode.Off;screenRenderer.receiveShadows=false;screen.SetActive(false);
        }
        public WorkstationSurface Choose(Camera camera)
        {
            if(camera==null)return null;WorkstationSurface chosen=null;float closest=25;
            foreach(var item in surfaces)
            {
                if(item==null||!item.Valid)continue;
                var offset=camera.transform.position-item.Centre;float distance=offset.sqrMagnitude;
                if(distance>closest||Vector3.Dot(offset,item.Normal)<=0)continue;
                var point=camera.WorldToViewportPoint(item.Centre);if(point.z<=0||point.x<-.1f||point.x>1.1f||point.y<-.1f||point.y>1.1f)continue;
                // Walls occlude the physical quad normally; don't spend UI work on a screen in another room.
                if(Physics.Linecast(camera.transform.position,item.Centre,out var hit,Hostfall.Core.CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore)&&!hit.transform.IsChildOf(item.transform))continue;
                closest=distance;chosen=item;
            }
            return chosen;
        }
        public void PresentNativeGlass(bool focused,Camera camera)
        {
            bool visible=focused&&Surface!=null&&camera!=null;nativeGlass.style.display=visible?DisplayStyle.Flex:DisplayStyle.None;
            screenRenderer.enabled=!visible;
            if(!visible)return;
            var root=sourceDocument.rootVisualElement;
            Vector2 Project(Vector3 p){var screenPoint=camera.WorldToScreenPoint(p);return RuntimePanelUtils.ScreenToPanel(root.panel,new Vector2(screenPoint.x,Screen.height-screenPoint.y));}
            var tl=Project(Surface.TopLeft);var br=Project(Surface.BottomRight);nativeGlass.style.left=tl.x;nativeGlass.style.top=tl.y;nativeGlass.style.width=br.x-tl.x;nativeGlass.style.height=br.y-tl.y;
        }
        public Vector2 ScreenToPanel(Vector2 point)
        {
            var camera=Camera.main;if(!Interactive||Surface==null||camera==null)return new Vector2(-10000,-10000);
            var ray=camera.ScreenPointToRay(new Vector3(point.x,Screen.height-point.y,0));
            var plane=new Plane(Surface.Normal,Surface.Centre+Surface.Normal*.012f);
            if(!plane.Raycast(ray,out float distance)||distance>10)return new Vector2(-10000,-10000);
            var delta=ray.GetPoint(distance)-(Surface.TopLeft+Surface.Normal*.012f);var u=Surface.TopRight-Surface.TopLeft;var v=Surface.BottomLeft-Surface.TopLeft;
            var result=new Vector2(Vector3.Dot(delta,u)/u.sqrMagnitude*Width,Vector3.Dot(delta,v)/v.sqrMagnitude*Height);
            return result.x>=0&&result.x<=Width&&result.y>=0&&result.y<=Height?result*settings.scale:new Vector2(-10000,-10000);
        }
        public bool Select(WorkstationSurface surface)
        {
            if(Surface==surface){View.style.display=surface!=null?DisplayStyle.Flex:DisplayStyle.None;if(surface!=null)surface.SetNativeOverlay(true);return false;}
            if(Surface!=null)Surface.SetNativeOverlay(false);Surface=surface;
            screen.SetActive(surface!=null);View.style.display=surface!=null?DisplayStyle.Flex:DisplayStyle.None;
            if(surface==null)return true;
            surface.SetNativeOverlay(true);View.EnableInClassList("terminal-mac",surface.Os==Hostfall.Simulation.TerminalOsFamily.MacOsZsh);View.EnableInClassList("terminal-windows",surface.Os==Hostfall.Simulation.TerminalOsFamily.WindowsPowerShell);
            var n=surface.Normal*.012f;mesh.Clear();mesh.vertices=new[]{surface.TopLeft+n,surface.TopRight+n,surface.BottomLeft+n,surface.BottomRight+n};mesh.uv=new[]{new Vector2(0,1),new Vector2(1,1),new Vector2(0,0),new Vector2(1,0)};mesh.triangles=new[]{0,1,2,2,1,3};mesh.RecalculateBounds();mesh.RecalculateNormals();return true;
        }
        public void Dispose()
        {
            if(Surface!=null)Surface.SetNativeOverlay(false);
            nativeGlass.RemoveFromHierarchy();UnityEngine.Object.Destroy(owner);UnityEngine.Object.Destroy(screen);UnityEngine.Object.Destroy(settings);UnityEngine.Object.Destroy(material);UnityEngine.Object.Destroy(mesh);texture.Release();UnityEngine.Object.Destroy(texture);
        }
    }
}
