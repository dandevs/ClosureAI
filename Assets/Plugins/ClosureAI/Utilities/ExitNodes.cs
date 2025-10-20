#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClosureAI
{
    public static partial class AI
    {
        /// <summary>
        /// Exits a sequence of nodes one after another, waiting for each node to fully exit before proceeding to the next.
        /// </summary>
        /// <param name="tree">The behavior tree runner instance</param>
        /// <param name="ct">Cancellation token to abort the operation</param>
        /// <param name="nodes">List of nodes to exit</param>
        /// <param name="startIndexInclusive">The starting index in the nodes list (inclusive)</param>
        /// <param name="endIndexInclusive">The ending index in the nodes list (inclusive)</param>
        /// <returns>UniTask that completes when all nodes have been exited</returns>
        public static async UniTask ResetNodesSequential(List<Node> nodes, int startIndexInclusive, int endIndexInclusive)
        {
            if (nodes.Count == 0)
                return;

            var step = endIndexInclusive > startIndexInclusive ? 1 : -1;

            for (var i = startIndexInclusive; step > 0 ? i <= endIndexInclusive : i >= endIndexInclusive; i += step)
            {
                while (!nodes[i].ResetGracefully())
                    await UniTask.Yield();
            }
        }

        public static bool ResetNodesSequentialNonAsync(List<Node> nodes, int startIndexInclusive, int endIndexInclusive)
        {
            if (nodes.Count == 0)
                return false;

            var step = endIndexInclusive > startIndexInclusive ? 1 : -1;

            for (var i = startIndexInclusive; step > 0 ? i <= endIndexInclusive : i >= endIndexInclusive; i += step)
            {
                while (!nodes[i].ResetGracefully())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Resets a single node gracefully.
        /// </summary>
        /// <param name="tree">The behavior tree runner instance</param>
        /// <param name="node">The node to exit</param>
        /// <param name="ct">Cancellation token to abort the operation</param>
        /// <returns>UniTask that completes when the node has been exited</returns>
        public static async UniTask ResetNode(Node node)
        {
            while (!node.ResetGracefully())
                await UniTask.Yield();
        }

        /// <summary>
        /// Exits single node and waits until it's fully exited.
        /// </summary>
        /// <param name="node">The node to exit</param>
        /// <returns>UniTask that completes when the node has been exited</returns>
        public static async UniTask ExitNode(Node node)
        {
            while (!node.Exit())
                await UniTask.Yield();
        }

        public static async UniTask ExitNodesSequential(List<Node> nodes, int startIndexInclusive, int endIndexInclusive)
        {
            if (nodes.Count == 0)
                return;

            var step = endIndexInclusive > startIndexInclusive ? 1 : -1;

            for (var i = startIndexInclusive; step > 0 ? i <= endIndexInclusive : i >= endIndexInclusive; i += step)
            {
                while (!nodes[i].Exit())
                    await UniTask.Yield();
            }
        }

        /// <summary>
        /// Attempts to exit multiple nodes simultaneously, checking each node every frame until all are exited.
        /// </summary>
        /// <param name="nodes">List of nodes to exit</param>
        /// <param name="startIndexInclusive">The starting index in the nodes list (inclusive)</param>
        /// <param name="endIndexInclusive">The ending index in the nodes list (inclusive)</param>
        /// <returns>UniTask that completes when all nodes have been exited</returns>
        public static async UniTask ExitNodesParallel(List<Node> nodes, int startIndexInclusive, int endIndexInclusive)
        {
            var step = endIndexInclusive > startIndexInclusive ? 1 : -1;

            while (true)
            {
                var done = 0;

                for (var i = startIndexInclusive; step > 0 ? i <= endIndexInclusive : i >= endIndexInclusive; i += step)
                {
                    if (nodes[i].Exit())
                        done++;
                }

                if (done == Mathf.Abs(endIndexInclusive - startIndexInclusive) + 1)
                    break;
                else
                    await UniTask.Yield();
            }
        }

        //*****************************************************************************************************************

        /// <summary>
        /// Attempts to exit multiple nodes simultaneously, checking each node every frame until all are exited.
        /// </summary>
        /// <param name="nodes">List of nodes to exit</param>
        /// <param name="startIndexInclusive">The starting index in the nodes list (inclusive)</param>
        /// <param name="endIndexInclusive">The ending index in the nodes list (inclusive)</param>
        /// <returns>UniTask that completes when all nodes have been exited</returns>
        public static async UniTask ResetNodesParallel(List<Node> nodes, int startIndexInclusive, int endIndexInclusive)
        {
            var step = endIndexInclusive > startIndexInclusive ? 1 : -1;

            while (true)
            {
                var done = 0;

                for (var i = startIndexInclusive; step > 0 ? i <= endIndexInclusive : i >= endIndexInclusive; i += step)
                {
                    if (nodes[i].Exit())
                        done++;
                }

                if (done == Mathf.Abs(endIndexInclusive - startIndexInclusive) + 1)
                    break;
                else
                    await UniTask.Yield();
            }
        }
    }
}

#endif