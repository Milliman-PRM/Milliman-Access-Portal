/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE: A basic, generic tree structure for data representation in view models.
 * DEVELOPER NOTES:
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace MapCommonLib
{
    /// <summary>
    /// Represents a basic tree with minimal features.
    /// </summary>
    /// <typeparam name="T">Type of value to be stored at each tree node</typeparam>
    public class BasicTree<T> where T: Nestable
    {
        public BasicNode<T> Root { get; set; } = new BasicNode<T>();
    }

    /// <summary>
    /// Represents a node in a BasicTree
    /// </summary>
    /// <typeparam name="T">Type of value to be stored at this node</typeparam>
    public class BasicNode<T> where T: Nestable
    {
        public T Value { get; set; }
        public List<BasicNode<T>> Children { get; set; } = new List<BasicNode<T>>();

        /// <summary>
        /// Fill in child nodes with the provided nestable values
        /// </summary>
        /// <param name="values">List of values to add as children recursively</param>
        public void Populate(ref List<T> values)
        {
            Populate(ref values, Value?.Id);
        }

        /// <summary>
        /// Fill in child nodes with the provided nestable values (private recursive method)
        /// </summary>
        /// <param name="values">List of values to add as children recursively</param>
        /// <param name="parentId">ID of the current value</param>
        private void Populate(ref List<T> values, Guid? parentId)
        {
            var nodeValues = values.Where(v => v.ParentId == parentId).ToList();
            values.RemoveAll(v => nodeValues.Contains(v));

            foreach (var nodeValue in nodeValues)
            {
                var childNode = new BasicNode<T>
                {
                    Value = nodeValue,
                };
                childNode.Populate(ref values, nodeValue.Id);
                Children.Add(childNode);
            }
        }

        /// <summary>
        /// Prune subtrees
        /// </summary>
        /// <param name="map">Function to apply to each subtree's child node values</param>
        /// <param name="reduce">Function to aggregate map results</param>
        /// <param name="seed">Starting value for reduce function</param>
        public void Prune(Func<T, bool> map, Func<bool, bool, bool> reduce, bool seed)
        {
            List<BasicNode<T>> flatten(BasicNode<T> node)
            {
                var flattenedNodes = new List<BasicNode<T>>();
                foreach (var childNode in node.Children)
                {
                    flattenedNodes = flattenedNodes.Concat(flatten(childNode)).ToList();
                }
                flattenedNodes.Add(node);
                return flattenedNodes;
            }

            foreach (var child in Children)
            {
                child.Prune(map, reduce, seed);
            }
            var flattened = Children.ToDictionary(c => c, c => flatten(c));
            Children.RemoveAll(node => !flattened[node].Select(n => map(n.Value)).Aggregate(seed, reduce));
        }

        /// <summary>
        /// Apply a function recursively to this node and all children
        /// </summary>
        /// <param name="map">Function to apply to each subtree's child node values</param>
        public void Apply(Func<T, T> map)
        {
            Value = map(Value);
            foreach (var childNode in Children)
            {
                childNode.Apply(map);
            }
        }

        /// <summary>
        /// Order a tree's children recursively
        /// </summary>
        /// <remarks>A more robust approach would be to implement IOrderedEnumerable</remarks>
        /// <typeparam name="U"></typeparam>
        /// <param name="lambda">Provides the element used to sort the tree.</param>
        public void OrderInPlaceBy<U>(Func<BasicNode<T>, U> lambda)
        {
            Children = Children.OrderBy(lambda).ToList();
            foreach (var childNode in Children)
            {
                childNode.OrderInPlaceBy(lambda);
            }
        }
    }

    /// <summary>
    /// Denotes an element that can have a parent element
    /// </summary>
    public abstract class Nestable
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; } = null;
    }
}
