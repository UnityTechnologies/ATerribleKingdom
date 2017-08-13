using System;
using System.Collections.Generic;
using System.Text;
using GraphVisualizer;
using UnityEngine.Playables;

public class SharedPlayableNode : Node
{
    public SharedPlayableNode(object content, float weight = 1, bool active = false)
        : base(content, weight, active)
    {
    }

    protected static string InfoString(string key, double value)
    {
        return String.Format(
            ((Math.Abs(value) < 100000.0) ? "<b>{0}:</b> {1:#.###}" : "<b>{0}:</b> {1:E4}"), key, value);
    }

    protected static string InfoString(string key, int value)
    {
        return String.Format("<b>{0}:</b> {1:D}", key, value);
    }

    protected static string InfoString(string key, object value)
    {
        return "<b>" + key + ":</b> " + (value ?? "(none)");
    }

    protected static string RemoveFromEnd(string str, string suffix)
    {
        if (str.EndsWith(suffix))
        {
            return str.Substring(0, str.Length - suffix.Length);
        }
        return str;
    }
}

public class PlayableNode : SharedPlayableNode
{
    public PlayableNode(Playable content, float weight = 1, bool active = false)
        : base(content, weight, active)
    {
    }

    public override Type GetContentType()
    {
        Playable p = Playable.Null;
        try
        {
            p = ((Playable)content);
        }
        catch
        {
            // Ignore.
        }
        return !p.IsValid() ? null : p.GetPlayableType();
    }

    public override string GetContentTypeShortName()
    {
        // Remove the extra Playable at the end of the Playable types.
        string shortName = base.GetContentTypeShortName();
        string cleanName = RemoveFromEnd(shortName, "Playable");
        return string.IsNullOrEmpty(cleanName) ? shortName : cleanName;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine(InfoString("Handle", GetContentTypeShortName()));

        var h = (Playable)content;

        sb.AppendLine(InfoString("IsValid", h.IsValid()));

        if (h.IsValid())
        {
            sb.AppendLine(InfoString("IsDone", h.IsDone()));
            sb.AppendLine(InfoString("InputCount", h.GetInputCount()));
            sb.AppendLine(InfoString("OutputCount", h.GetOutputCount()));
            sb.AppendLine(InfoString("PlayState", h.GetPlayState()));
            sb.AppendLine(InfoString("Speed", h.GetSpeed()));
            sb.AppendLine(InfoString("Duration", h.GetDuration()));
            sb.AppendLine(InfoString("Time", h.GetTime()));
            //        sb.AppendLine(InfoString("Animation", h.animatedProperties));
        }

        return sb.ToString();
    }
}

public class PlayableOutputNode : SharedPlayableNode
{
    public PlayableOutputNode(PlayableOutput content)
        : base(content, content.GetWeight(), true)
    {
    }

    public override Type GetContentType()
    {
        PlayableOutput p = PlayableOutput.Null;
        try
        {
            p = ((PlayableOutput)content);
        }
        catch
        {
            // Ignore.
        }
        return !p.IsOutputValid() ? null : p.GetPlayableOutputType();
    }

    public override string GetContentTypeShortName()
    {
        // Remove the extra Playable at the end of the Playable types.
        string shortName = base.GetContentTypeShortName();
        string cleanName = RemoveFromEnd(shortName, "PlayableOutput") + "Output";
        return string.IsNullOrEmpty(cleanName) ? shortName : cleanName;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine(InfoString("Handle", GetContentTypeShortName()));

        var h = (PlayableOutput)content;

        sb.AppendLine(InfoString("IsValid", h.IsOutputValid()));

        return sb.ToString();
    }
}

public class PlayableGraphVisualizer : Graph
{
    private PlayableGraph m_PlayableGraph;

    public PlayableGraphVisualizer(PlayableGraph playableGraph)
    {
        m_PlayableGraph = playableGraph;
    }

    protected override void Populate()
    {
        if (!m_PlayableGraph.IsValid())
            return;

        int outputs = m_PlayableGraph.GetOutputCount();
        for (int i = 0; i < outputs; i++)
        {
            var output = m_PlayableGraph.GetOutput(i);
            if(output.IsOutputValid())
            {
                AddNodeHierarchy(CreateNodeFromPlayableOutput(output));
            }
        }
    }

    protected override IEnumerable<Node> GetChildren(Node node)
    {
        // Children are the Playable Inputs.
        if(node is PlayableNode)
            return GetInputsFromPlayableNode((Playable)node.content);
        else if(node is PlayableOutputNode)
            return GetInputsFromPlayableOutputNode((PlayableOutput)node.content);

        return new List<Node>();     
    }

    private List<Node> GetInputsFromPlayableNode(Playable h)
    {
        var inputs = new List<Node>();
        if (h.IsValid())
        {
            for (int port = 0; port < h.GetInputCount(); ++port)
            {
                Playable playable = h.GetInput(port);
                if (playable.IsValid())
                {
                    float weight = h.GetInputWeight(port);
                    Node node = CreateNodeFromPlayable(playable, weight);
                    inputs.Add(node);
                }
            }
        }
        return inputs;
    }

    private List<Node> GetInputsFromPlayableOutputNode(PlayableOutput h)
    {
        var inputs = new List<Node>();
        if (h.IsOutputValid())
        {            
            Playable playable = h.GetSourcePlayable();
            if (playable.IsValid())
            {
                Node node = CreateNodeFromPlayable(playable, 1);
                inputs.Add(node);
            }
        }
        return inputs;
    }

    private PlayableNode CreateNodeFromPlayable(Playable h, float weight)
    {
        return new PlayableNode(h, weight, h.GetPlayState() == PlayState.Playing);
    }

    private PlayableOutputNode CreateNodeFromPlayableOutput(PlayableOutput h)
    {
        return new PlayableOutputNode(h);
    }

    private static bool HasValidOuputs(Playable h)
    {
        for (int port = 0; port < h.GetOutputCount(); ++port)
        {
            Playable playable = h.GetOutput(port);
            if (playable.IsValid())
            {
                return true;
            }
        }
        return false;
    }
}
