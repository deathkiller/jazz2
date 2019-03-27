using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Duality.IO;

namespace Jazz2.Storage.Content
{
    public class ContentTree : IEnumerable<ContentTree.Node>, IEnumerable
    {
        public class Node : IEnumerable<Node>, IEnumerable
        {
            private readonly string name;
            private readonly Node parent;
            private readonly Dictionary<string, Node> children = new Dictionary<string, Node>();

            public Node Parent
            {
                get
                {
                    return this.parent;
                }
            }

            public ICollection<Node> Children
            {
                get
                {
                    return this.children.Values;
                }
            }

            public string Name
            {
                get
                {
                    return this.name;
                }
            }

            public ResourceSource Source { get; set; }

            public Node this[string name]
            {
                get
                {
                    Node node;
                    this.children.TryGetValue(name, out node);
                    return node;
                }
            }

            public Node()
            {
            }

            public Node(Node parent, string name, ResourceSource resource = null)
            {
                this.parent = parent;
                this.name = name;
                this.Source = resource;
            }

            public Node TryAdd(string name)
            {
                Node node;
                if (!this.children.TryGetValue(name, out node)) {
                    node = new Node(this, name);
                    this.children.Add(name, node);
                }
                return node;
            }

            public void Remove(string name)
            {
                this.children.Remove(name);
            }

            public IEnumerator<Node> GetEnumerator()
            {
                return this.children.Values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            public override string ToString()
            {
                return this.name;
            }
        }

        private readonly Node root = new Node();

        public Node Root
        {
            get
            {
                return this.root;
            }
        }

        public Node this[string fullPath]
        {
            get
            {
                if (string.IsNullOrEmpty(fullPath)) {
                    return null;
                }

                Node node = this.root;
                string[] parts = fullPath.Split(PathOp.DirectorySeparatorChar);
                for (int i = 0; i < parts.Length; i++) {
                    if ((node = node[parts[i]]) == null) {
                        break;
                    }
                }

                return node;
            }
        }

        public Node AddNodeByPath(string fullPath)
        {
            Node node = this.root;
            string[] parts = fullPath.Split(PathOp.DirectorySeparatorChar);
            for (int i = 0; i < parts.Length; i++) {
                node = node.TryAdd(parts[i]);
            }

            return node;
        }

        public void RemoveEmptyNodes()
        {
            RemoveEmptyNodes(this.root);
        }

        private void RemoveEmptyNodes(Node node)
        {
            foreach (Node children in node.ToArray()) {
                RemoveEmptyNodes(children);
            }

            if (node.Parent != null) {
                if (node.Source == null && node.Children.Count == 0) {
                    node.Parent.Remove(node.Name);
                }
            }
        }

        public void MergeWith(ContentTree tree)
        {
            foreach (KeyValuePair<string, Node> pair in tree.GetNodeListing()) {
                Node node = AddNodeByPath(pair.Key);
                if (pair.Value.Source != null) {
                    node.Source = pair.Value.Source;
                }
            }
        }

        public IDictionary<string, Node> GetNodeListing()
        {
            Dictionary<string, Node> listing = new Dictionary<string, Node>();

            Stack<Tuple<Node, string>> stack = new Stack<Tuple<Node, string>>();
            stack.Push(Tuple.Create(this.root, ""));

            do {
                Tuple<Node, string> item = stack.Pop();
                string path = (string.IsNullOrEmpty(item.Item2) ? "" : (item.Item2 + PathOp.DirectorySeparatorChar)) + item.Item1.Name;

                listing[path] = item.Item1;

                foreach (Node node in item.Item1) {
                    stack.Push(Tuple.Create(node, path));
                }
            } while (stack.Count > 0);

            return listing;
        }

        public void GetContentFromDirectory(string path, string currentFullPath = null, bool includePath = true)
        {
            if (!string.IsNullOrEmpty(currentFullPath)) {
                if (currentFullPath[currentFullPath.Length - 1] != PathOp.DirectorySeparatorChar) {
                    currentFullPath += PathOp.DirectorySeparatorChar;
                }
            } else {
                currentFullPath = string.Empty;
            }

            if (includePath) {
                currentFullPath += Path.GetFileName(path);
            }

            foreach (string file in Directory.EnumerateFiles(path)) {
                FileInfo info = new FileInfo(file);
                if (info.Name.Contains(".")) {
                    string fullKeyPath = (info.Name == PathOp.DirectorySeparatorChar.ToString() ? currentFullPath : (currentFullPath + PathOp.DirectorySeparatorChar + info.Name));

                    Node node = this.AddNodeByPath(fullKeyPath);
                    node.Source = new FileResourceSource(file, 0, info.Length, false);
                }
            }

            foreach (string subfolders in Directory.GetDirectories(path)) {
                GetContentFromDirectory(subfolders, currentFullPath);
            }
        }

        public IEnumerator<Node> GetEnumerator()
        {
            Stack<Node> stack = new Stack<Node>();
            stack.Push(this.root);

            do {
                Node parent = stack.Pop();

                yield return parent;
                foreach (Node node in parent) {
                    stack.Push(node);
                }
            } while (stack.Count > 0);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}