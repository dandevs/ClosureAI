#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClosureAI
{
    public static partial class AI
    {
        public static partial class D
        {
            /// <summary>
            /// Creates a decorator that iterates over a collection, running its child node once for each item.
            /// The current item is exposed via an out parameter, allowing child nodes to access it.
            /// This variant also applies a mapping function to transform items before exposing them.
            /// </summary>
            /// <typeparam name="T">The type of items in the source collection</typeparam>
            /// <typeparam name="R">The type of the mapped/transformed value exposed to children</typeparam>
            /// <param name="name">The name of the decorator node for debugging and visualization</param>
            /// <param name="getEnumerable">A function that returns the collection to iterate over</param>
            /// <param name="map">A function that transforms each item from type T to type R</param>
            /// <param name="getCurrentValue">Output parameter that provides a function to get the current mapped item</param>
            /// <returns>A decorator node that iterates over the collection</returns>
            /// <remarks>
            /// <para><b>Behavior:</b></para>
            /// <list type="bullet">
            /// <item>Evaluates getEnumerable() on OnEnter to get the collection</item>
            /// <item>Copies collection items into an internal list for stable iteration</item>
            /// <item>Sets current value to first item (after mapping)</item>
            /// <item>Ticks the child for the current item</item>
            /// <item>If child fails, entire ForEach fails immediately (short-circuits)</item>
            /// <item>When child completes successfully, resets child and moves to next item</item>
            /// <item>Updates current value to next item (after mapping)</item>
            /// <item>Returns <see cref="Status.Success"/> when all items have been processed</item>
            /// <item>Returns <see cref="Status.Failure"/> if any iteration fails</item>
            /// <item>Returns <see cref="Status.Running"/> while iterating</item>
            /// <item>Exits and resets the child when decorator exits</item>
            /// <item>Invalidates when the child invalidates</item>
            /// </list>
            ///
            /// <para><b>Common Use Cases:</b></para>
            /// <list type="bullet">
            /// <item>Processing lists of targets (e.g., "attack each enemy in range")</item>
            /// <item>Multi-item interactions (e.g., "collect each resource in area")</item>
            /// <item>Batch operations (e.g., "upgrade each building in list")</item>
            /// <item>Sequential task processing (e.g., "complete each objective")</item>
            /// <item>Transforming collections (map function) before processing</item>
            /// </list>
            ///
            /// <para><b>Example - Attack Each Enemy:</b></para>
            /// <code>
            /// D.ForEach(() => enemiesInRange, out var getCurrentEnemy);
            /// Sequence("Attack One", () =>
            /// {
            ///     var target = Variable(() => getCurrentEnemy());
            ///     MoveTo(() => target.Value.position);
            ///     Do(() => Attack(target.Value));
            /// });
            /// // Attacks each enemy in range sequentially
            /// </code>
            ///
            /// <para><b>Example - Collect Items with Mapping:</b></para>
            /// <code>
            /// D.ForEach(
            ///     () => itemsNearby,
            ///     item => item.transform.position, // Map to position
            ///     out var getCurrentPosition
            /// );
            /// Sequence("Collect One", () =>
            /// {
            ///     MoveTo(getCurrentPosition);
            ///     Do(() => PickUpItem());
            /// });
            /// // Moves to each item's position and collects it
            /// </code>
            ///
            /// <para><b>Example - Process Waypoints:</b></para>
            /// <code>
            /// D.ForEach(() => waypoints, out var getCurrentWaypoint);
            /// Sequence("Visit Waypoint", () =>
            /// {
            ///     MoveTo(getCurrentWaypoint);
            ///     Wait(2f);
            ///     Do(() => OnWaypointReached());
            /// });
            /// // Visits each waypoint in sequence with 2-second delay
            /// </code>
            ///
            /// <para><b>Example - Failed Iteration:</b></para>
            /// <code>
            /// D.ForEach(() => targets, out var getCurrentTarget);
            /// Sequence("Process Target", () =>
            /// {
            ///     Condition(() => ValidateTarget(getCurrentTarget()));
            ///     Do(() => ProcessTarget(getCurrentTarget()));
            /// });
            /// // If any target fails validation, entire ForEach fails (short-circuits)
            /// </code>
            ///
            /// <para><b>Collection Evaluation:</b></para>
            /// <list type="bullet">
            /// <item>getEnumerable() is called once on OnEnter</item>
            /// <item>Collection is copied into internal list at that time</item>
            /// <item>Changes to original collection after OnEnter don't affect iteration</item>
            /// <item>This prevents issues with collection modification during iteration</item>
            /// </list>
            ///
            /// <para><b>Current Value Access:</b></para>
            /// <list type="bullet">
            /// <item>getCurrentValue returns a Func&lt;R&gt; that can be called to get current item</item>
            /// <item>Value is updated after each successful iteration</item>
            /// <item>Value is dereferenced (set to default) on OnExit</item>
            /// <item>Can be passed to child nodes via Variable(() => getCurrentValue())</item>
            /// </list>
            ///
            /// <para><b>Mapping Function:</b></para>
            /// The map function transforms each item before exposing it:
            /// <list type="bullet">
            /// <item>Called once per item when setting current value</item>
            /// <item>Useful for extracting properties (e.g., GameObject ? Transform)</item>
            /// <item>Useful for type conversions or data transformations</item>
            /// </list>
            ///
            /// <para><b>Short-Circuit on Failure:</b></para>
            /// If the child returns Failure for any item, the ForEach immediately returns Failure
            /// without processing remaining items. This is similar to Sequence behavior.
            ///
            /// <para><b>Child Reset Between Items:</b></para>
            /// The child is reset via ResetGracefully() after each successful completion to ensure
            /// clean state for the next item. Variables are re-initialized, lifecycle callbacks reset.
            ///
            /// <para><b>Decorator Pattern Usage:</b></para>
            /// This is a decorator, so it must be placed BEFORE the node it decorates:
            /// <code>
            /// D.ForEach(() => myList, x => x.value, out var getCurrent);
            /// MyChildNode();
            /// </code>
            /// </remarks>
            public static DecoratorNode ForEach<T, R>(string name, Func<IEnumerable<T>> getEnumerable, Func<T, R> map, out Func<R> getCurrentValue)
            {
                VariableType<R> current = null;
                getCurrentValue = () => current.Value;

                return Decorator("For Each", () =>
                {
                    var node = (DecoratorNode)CurrentNode;
                    var _index = Variable(0, static () => 0);
                    current = Variable<R>();
                    var _list = Variable(new List<T>());

                    SetNodeName(name);
                    OnInvalidCheck(() => node.Child.IsInvalid());

                    OnEnter(() =>
                    {
                        _list.Value.AddRange(getEnumerable());

                        if (_list.Value.Count > 0)
                            current.Value = map(_list.Value[0]);
                    });

                    OnExit(_ =>
                    {
                        current.Value = default; // Dereference value
                        _list.Value.Clear();
                        _index.Value = 0;
                        return ExitNode(node.Child);
                    });

                    OnBaseTick(() =>
                    {
                        var child = node.Child;

                        while (_index.Value < _list.Value.Count)
                        {
                            if (!child.Done)
                            {
                                if (child.Tick(out var status))
                                {
                                    if (status == Status.Failure)
                                        return Status.Failure;
                                }
                            }
                            else if (child.ResetGracefully())
                            {
                                _index.Value++;

                                if (_index.Value < _list.Value.Count)
                                    current.Value = map(_list.Value[_index.Value]);

                                continue;
                            }

                            return Status.Running;
                        }

                        // index.Value = Mathf.Min(index, list.Value.Count - 1);
                        return _index.Value >= _list.Value.Count ? Status.Success : Status.Failure;
                    });
                });
            }

            /// <summary>
            /// Creates a decorator that iterates over a collection, running its child node once for each item.
            /// The current item is exposed via an out parameter without transformation.
            /// </summary>
            /// <typeparam name="T">The type of items in the collection</typeparam>
            /// <param name="name">The name of the decorator node for debugging and visualization</param>
            /// <param name="getEnumerable">A function that returns the collection to iterate over</param>
            /// <param name="getCurrentValue">Output parameter that provides a function to get the current item</param>
            /// <returns>A decorator node that iterates over the collection</returns>
            /// <remarks>
            /// This is a convenience overload that doesn't apply any mapping - items are exposed directly.
            /// See <see cref="ForEach{T, R}(string, Func{IEnumerable{T}}, Func{T, R}, out Func{R})"/> for detailed behavior description.
            /// </remarks>
            public static DecoratorNode ForEach<T>(string name, Func<IEnumerable<T>> getEnumerable, out Func<T> getCurrentValue)
            {
                return ForEach(name, getEnumerable, static x => x, out getCurrentValue);
            }

            /// <summary>
            /// Creates a decorator that iterates over a collection, running its child node once for each item.
            /// Uses "For Each" as the default name. Applies a mapping function to transform items.
            /// </summary>
            /// <typeparam name="T">The type of items in the source collection</typeparam>
            /// <typeparam name="R">The type of the mapped/transformed value exposed to children</typeparam>
            /// <param name="getEnumerable">A function that returns the collection to iterate over</param>
            /// <param name="map">A function that transforms each item from type T to type R</param>
            /// <param name="getCurrentValue">Output parameter that provides a function to get the current mapped item</param>
            /// <returns>A decorator node that iterates over the collection</returns>
            /// <remarks>
            /// This is a convenience overload that uses "For Each" as the default node name.
            /// See <see cref="ForEach{T, R}(string, Func{IEnumerable{T}}, Func{T, R}, out Func{R})"/> for detailed behavior description.
            /// </remarks>
            public static DecoratorNode ForEach<T, R>(Func<IEnumerable<T>> getEnumerable, Func<T, R> map, out Func<R> getCurrentValue)
            {
                return ForEach("For Each", getEnumerable, map, out getCurrentValue);
            }

            /// <summary>
            /// Creates a decorator that iterates over a collection, running its child node once for each item.
            /// Uses "For Each" as the default name. Items are exposed directly without transformation.
            /// </summary>
            /// <typeparam name="T">The type of items in the collection</typeparam>
            /// <param name="getEnumerable">A function that returns the collection to iterate over</param>
            /// <param name="getCurrentValue">Output parameter that provides a function to get the current item</param>
            /// <returns>A decorator node that iterates over the collection</returns>
            /// <remarks>
            /// This is a convenience overload that uses "For Each" as the default node name and doesn't apply mapping.
            /// See <see cref="ForEach{T, R}(string, Func{IEnumerable{T}}, Func{T, R}, out Func{R})"/> for detailed behavior description.
            /// </remarks>
            public static DecoratorNode ForEach<T>(Func<IEnumerable<T>> getEnumerable, out Func<T> getCurrentValue)
            {
                return ForEach("For Each", getEnumerable, static x => x, out getCurrentValue);
            }
        }
    }
}

#endif