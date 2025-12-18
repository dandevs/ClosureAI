#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static ClosureBT.BT;

namespace ClosureBT
{
    public class NodeException : Exception
    {
        public readonly List<Node> NodeTrace = new();
        public Exception OriginalException;
        public Node FailedNode;
        public string Lifecycle;

        public NodeException(Node failedNode, Exception exception, string lifecycle = null) : base(exception.Message, exception)
        {
            OriginalException = exception;
            FailedNode = failedNode;
            Lifecycle = lifecycle;

            var _node = failedNode;

            do
            {
                NodeTrace.Add(_node);
                _node = _node.Parent;
            }
            while (_node != null);
        }

        public override string StackTrace
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine($"<b>({NodeTrace[0].Name})</b> {OriginalException.GetType()} occurred: {OriginalException.Message}");
                if (!string.IsNullOrEmpty(Lifecycle))
                    sb.AppendLine($"<b>During {Lifecycle}:</b>");
                sb.AppendLine(".");

                var trace = NodeTrace.Select((node, i) =>
                {
                    var prefix = $"* {node.Name.Trim()}";
                    return (Node: node, Prefix: prefix);
                });

                var maxNameLengths = trace
                    .Aggregate(0, (acc, name) => name.Prefix.Length > acc ? name.Prefix.Length : acc);

                foreach (var (node, prefix) in trace)
                {
                    sb.Append($"<b><mspace=0.55em>{prefix.PadRight(maxNameLengths + 3, ' ')}</mspace></b>");
                    sb.AppendLine(CreateVariableString(node));
                }

                sb.AppendLine(".");
                // sb.AppendLine(OriginalException?.StackTrace);

                return sb.ToString();
            }
        }

        public override string ToString()
        {
            return StackTrace;
        }

        private string CreateVariableString(Node node)
        {
            if (node?.Variables == null || !node.Variables.Any())
                return "";

            var variableStrings = node.Variables
                .Take(4)
                .Select(vbs =>
                {
                    var name = "?";

#if UNITY_EDITOR
                    name = vbs.Name;
#endif

                    var value = vbs.GetValueAsObject()?.ToString() ?? "null";
                    return $"[<b>{name}</b>: {value}]";
                })
                .ToList();

            return string.Join("  ", variableStrings);
        }

        private static string Ellipsis(int maxLength, string str)
        {
            return str.Length > maxLength ? str.Substring(0, maxLength - 3) + "..." : str;
        }
    }
}

#endif
