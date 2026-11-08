using System;
using System.Collections.Generic;
using System.Linq;
using Hostfall.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hostfall.Field
{
    public sealed class KnowledgeGraphView : VisualElement
    {
        private readonly VisualElement canvas = new();
        private readonly Dictionary<string, Button> nodes = new();
        private readonly FieldSession field;
        private readonly HashSet<string> visibleNodes = new();
        private string current = "terminal", domain = "Системы";
        private Vector2 pan, last;
        private float zoom = 1;
        private bool dragging;
        public int NodeCount => visibleNodes.Count;
        public int CatalogNodeCount => nodes.Count;
        public string Domain => domain;
        public KnowledgeGraphView(FieldSession session, Action<string> onSelect)
        {
            field = session; AddToClassList("knowledge-graph"); focusable = true;
            canvas.style.width = 900; canvas.style.height = 400;
            canvas.style.position = Position.Absolute; canvas.style.transformOrigin = new TransformOrigin(0, 0);
            Add(canvas); canvas.generateVisualContent += DrawEdges;
            foreach (var a in field.Catalog.Articles.Values)
            {
                string id = a.id; var button = new Button(() => onSelect(id)) { name = "node-" + id };
                var title = new Label(a.title) { pickingMode = PickingMode.Ignore }; title.AddToClassList("node-title"); button.Add(title);
                var state = new Label { name = "state", pickingMode = PickingMode.Ignore }; state.AddToClassList("node-state"); button.Add(state);
                button.AddToClassList("graph-node"); button.style.left = a.x; button.style.top = a.y;
                canvas.Add(button); nodes.Add(id, button);
            }
            RegisterCallback<PointerDownEvent>(ev =>
            {
                if (ev.button != 0 || (ev.target != this && ev.target != canvas)) return;
                dragging = true; last = ev.localPosition; this.CapturePointer(ev.pointerId); ev.StopPropagation();
            });
            RegisterCallback<PointerMoveEvent>(ev => { if (!dragging) return; Vector2 point = ev.localPosition; pan += point - last; last = point; ApplyTransform(); });
            RegisterCallback<PointerUpEvent>(ev => { dragging = false; this.ReleasePointer(ev.pointerId); });
            RegisterCallback<PointerCaptureOutEvent>(_ => dragging = false);
            RegisterCallback<WheelEvent>(ev => { ChangeZoom(zoom * Mathf.Exp(-ev.delta.y * .06f), ev.localMousePosition); ev.StopPropagation(); });
            RegisterCallback<KeyDownEvent>(ev =>
            {
                Vector2 shift = ev.keyCode == KeyCode.LeftArrow ? Vector2.right : ev.keyCode == KeyCode.RightArrow ? Vector2.left :
                    ev.keyCode == KeyCode.UpArrow ? Vector2.up : ev.keyCode == KeyCode.DownArrow ? Vector2.down : Vector2.zero;
                if (shift == Vector2.zero) return; pan += shift * 32; ApplyTransform(); ev.StopPropagation();
            });
            RegisterCallback<GeometryChangedEvent>(_ => ResetView());
            SetDomain(domain); Refresh(current);
        }
        public void SetDomain(string value)
        {
            domain = value;
            Filter(field.Catalog.Articles.Values.Where(a => a.category == domain)); ResetView();
        }
        public void ZoomBy(float factor) => ChangeZoom(zoom * factor, contentRect.center);
        public void ResetView()
        {
            var articles = field.Catalog.Articles.Values.Where(a => a.category == domain).ToArray();
            if (articles.Length == 0 || contentRect.width <= 0 || contentRect.height <= 0) return;
            float left = articles.Min(a => a.x), top = articles.Min(a => a.y);
            float width = articles.Max(a => a.x) + 216 - left, height = articles.Max(a => a.y) + 90 - top;
            zoom = Mathf.Clamp(Mathf.Min((contentRect.width - 44) / width, (contentRect.height - 44) / height), .45f, 1.05f);
            pan = new Vector2((contentRect.width - width * zoom) / 2 - left * zoom, (contentRect.height - height * zoom) / 2 - top * zoom); ApplyTransform();
        }
        private void ChangeZoom(float next, Vector2 anchor)
        { next = Mathf.Clamp(next, .45f, 1.7f); pan = anchor - (anchor - pan) * (next / zoom); zoom = next; ApplyTransform(); }
        private void ApplyTransform()
        {
            pan.x = Mathf.Clamp(pan.x, -1300, 800); pan.y = Mathf.Clamp(pan.y, -700, 600);
            canvas.style.translate = new Translate(pan.x, pan.y); canvas.style.scale = new Scale(new Vector3(zoom, zoom, 1));
        }
        public void Filter(IEnumerable<KnowledgeArticle> articles)
        {
            visibleNodes.Clear(); foreach (var a in articles.Where(a => a.category == domain)) visibleNodes.Add(a.id);
            foreach (var pair in nodes) pair.Value.style.display = visibleNodes.Contains(pair.Key) ? DisplayStyle.Flex : DisplayStyle.None;
            canvas.MarkDirtyRepaint();
        }
        public void Refresh(string id)
        {
            current = id;
            var neighbors = field.Catalog.Data.links.Where(l => l.from == id || l.to == id).SelectMany(l => new[] { l.from, l.to }).ToHashSet();
            foreach (var pair in nodes)
            {
                pair.Value.EnableInClassList("node-selected", pair.Key == id);
                pair.Value.EnableInClassList("node-neighbor", neighbors.Contains(pair.Key) && pair.Key != id);
                pair.Value.EnableInClassList("node-practiced", field.State(pair.Key) >= KnowledgeState.Practiced);
                pair.Value.Q<Label>("state").text = StateLabel(field.State(pair.Key));
            }
            canvas.MarkDirtyRepaint();
        }
        private void DrawEdges(MeshGenerationContext context)
        {
            var painter = context.painter2D; var pairs = new HashSet<string>();
            foreach (var link in field.Catalog.Data.links)
            {
                if (!visibleNodes.Contains(link.from) || !visibleNodes.Contains(link.to)) continue;
                string key = string.CompareOrdinal(link.from, link.to) < 0 ? link.from + "/" + link.to : link.to + "/" + link.from;
                if (!pairs.Add(key)) continue;
                var a = field.Catalog.Articles[link.from]; var b = field.Catalog.Articles[link.to];
                bool right = b.x > a.x; Vector2 start = new(a.x + (right ? 216 : 0), a.y + 45), end = new(b.x + (right ? 0 : 216), b.y + 45);
                Vector2 firstControl, secondControl;
                if (Mathf.Abs(a.x - b.x) < 100)
                {
                    bool down = b.y > a.y;
                    start = new Vector2(a.x + 108, a.y + (down ? 90 : 0)); end = new Vector2(b.x + 108, b.y + (down ? 0 : 90));
                    Vector2 bend = Vector2.up * (down ? 35 : -35); firstControl = start + bend; secondControl = end - bend;
                }
                else
                {
                    float bend = Mathf.Max(35, Mathf.Abs(end.x - start.x) * .5f) * (right ? 1 : -1);
                    firstControl = start + Vector2.right * bend; secondControl = end - Vector2.right * bend;
                }
                painter.strokeColor = link.from == current || link.to == current ? new Color(.7f, .57f, .38f) : new Color(.25f, .36f, .4f);
                painter.lineWidth = 2; painter.BeginPath(); painter.MoveTo(start);
                painter.BezierCurveTo(firstControl, secondControl, end); painter.Stroke();
            }
        }
        public static string StateLabel(KnowledgeState state) => state switch
        {
            KnowledgeState.Discovered => "Открыто", KnowledgeState.Studied => "Прочитано", KnowledgeState.Practiced => "Опробовано",
            KnowledgeState.Applied => "Применено", _ => "Можно изучить"
        };
        public static string RelationLabel(string relation) => relation switch
        {
            "Requires" => "требует", "RelatedTo" => "связано с", "UsedBy" => "используется в", "PartOf" => "входит в",
            "Implements" => "реализует", "ContrastsWith" => "отличается от", _ => relation
        };
    }
}
