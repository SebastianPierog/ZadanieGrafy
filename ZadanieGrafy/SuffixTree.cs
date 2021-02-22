using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace ZadanieGrafy
{
    class SuffixTree
    {
        private const int BOUNDLESS = -1;

        private ActivePoint _AP;
        private int _remainder, _position = -1;
        private Node _root, _needSuffixLink;
        private List<char> _chars = new List<char>();
        private Dictionary<(Node, char), Node> _structure = new Dictionary<(Node, char), Node>();
        private Dictionary<Node, Node> _suffixLinks = new Dictionary<Node, Node>();

        private class Node
        {
            public int Start, End;
            //public Node Link; >> refactored to use Dictionary<Node, Node> _suffixLinks 
            public bool IsLeaf => End == BOUNDLESS;
        }

        private class ActivePoint
        {
            public Node ActiveEdge { get => _activeEdge; set => _activeEdge = value; }
            public Node ActiveParent { get => _activeParent; set => _activeParent = !value.IsLeaf ? value : throw new Exception("Leaf node cannot be parent."); }
            public int ActiveLength { get; private set; }

            private Node _activeEdge;
            private Node _activeParent;
            private SuffixTree _tree;

            public ActivePoint(SuffixTree tree)
                => _tree = tree;

            public void ResetEdge()
            {
                ActiveLength = 0;
                ActiveEdge = null;
            }

            public bool MoveDown(char c)
            {
                if (ActiveEdge == null &&
                    !_tree.GetEdgeFor(ActiveParent, c, out _activeEdge))
                    return false; // Cannot lock on edge

                else if (_tree._chars[ActiveEdge.Start + ActiveLength] != c)
                    return false; // Cannot match next char on edge

                ActiveLength++; // Success matching. Simply locking on a new edge is a match too, since it equals a first char match.

                if (!ActiveEdge.IsLeaf && ActiveLength == _tree.LengthOf(ActiveEdge))
                {
                    ActiveParent = ActiveEdge;
                    ResetEdge();
                }

                return true;
            }

            public void Rescan()
            {
                // If we can't jump to linked node, we need to jump to root, and use Remainder as ActiveLength
                if (!_tree.GetLinkFor(_activeParent, out _activeParent))
                {
                    ActiveEdge = null;
                    ActiveParent = _tree._root;
                    ActiveLength = _tree._remainder - 1;
                }

                if (ActiveLength == 0)
                    return;

                // Keep jumping through edges until we find the first edge that is shorter than our remaining length
                while (_tree.GetEdgeFor(ActiveParent, _tree._chars[_tree._position - ActiveLength], out _activeEdge)
                    && ActiveLength >= _tree.LengthOf(_activeEdge))
                {
                    ActiveLength -= _tree.LengthOf(_activeEdge);
                    ActiveParent = _activeEdge;
                }
            }
        }

        public SuffixTree()
        {
            _root = new Node() { Start = 0, End = 0 };
            _structure.Add((null, default(char)), _root);
            _AP = new ActivePoint(this) { ActiveParent = _root };
        }

        /// <summary>
        /// Creates and returns a tree with the specified value.
        /// Shorthand for separately instantiating and adding a string.
        /// </summary>
        public static SuffixTree Build(string value)
        {
            var t = new SuffixTree();
            t.AddString(value);
            return t;
        }

        /// <summary>
        /// Extends the suffix tree with the specified value.
        /// </summary>
        public void AddString(string value)
        {
            // TODO: What about terminating with unique character?
            foreach (var c in value)
                ExtendTree(c);

            _remainder = 0;
            _AP.ResetEdge();
            _AP.ActiveParent = _root;
        }

        //MOJE, wczytuje stringa z konsoli
        public string ReadString()
        {
            Console.WriteLine("Podaj ciag znakow:");
            string value = "^[]";
            value += Console.ReadLine();
            value += "^[]";
            return value;
        }

        public string ReadStringV2(string arg)
        {
            if (!File.Exists(".//" + arg))
            {
                Console.WriteLine("Nie ma takiego pliku! Podaj nazwe pliku i rozszerzenie");
                return null;
            }
            string value = File.ReadAllText(".//" + arg);
            return value + "$";
        }

        private void ExtendTree(char c)
        {
            _chars.Add(c);
            _needSuffixLink = null;
            _position++;
            _remainder++;

            while (_remainder > 0)
            {
                if (_AP.MoveDown(c))
                    break;

                if (_AP.ActiveEdge != null)
                    _AP.ActiveParent = InsertSplit(_AP);

                InsertLeaf(_AP, c);
                _remainder--;

                if (_remainder > 0)
                    _AP.Rescan();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int LengthOf(Node edge)
            => (edge.End == -1 ? _position + 1 : edge.End) - edge.Start;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private char FirstCharOf(Node edge)
            => _chars[edge.Start];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string LabelOf(Node edge)
        {
            var res = new char[LengthOf(edge)];
            _chars.CopyTo(edge.Start, res, 0, LengthOf(edge));
            return new string(res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GetLinkFor(Node node, out Node linkedNode)
            => _suffixLinks.TryGetValue(node, out linkedNode);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GetEdgeFor(Node n, char c, out Node edge)
            => _structure.TryGetValue((n, c), out edge);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Node InsertLeaf(ActivePoint ap, char c)
        {
            var node = new Node() { Start = _position, End = BOUNDLESS };
            _structure.Add((ap.ActiveParent, c), node);

            return node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Node InsertSplit(ActivePoint ap)
        {
            // Remove node to be split
            var splittingPointId = (ap.ActiveParent, FirstCharOf(ap.ActiveEdge));
            var splittable = ap.ActiveEdge;
            _structure.Remove(splittingPointId);

            // Insert new branch node in place of split node
            var branch = new Node() { Start = splittable.Start, End = splittable.Start + ap.ActiveLength };
            _structure.Add(splittingPointId, branch);
            _AP.ActiveEdge = branch;
            AddSuffixLink(branch);

            // Update split node, and reinsert as child of new branch
            splittable.Start = branch.End;
            _structure.Add((branch, FirstCharOf(splittable)), splittable);

            return branch;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddSuffixLink(Node node)
        {
            if (_needSuffixLink != null)
                _suffixLinks.Add(_needSuffixLink, node);

            _needSuffixLink = node;
        }

        /// <summary>
        /// Checks if the specified value is a substring of the tree content.
        /// Executes with O(n) time complexity, where n is the length of the value.
        /// </summary>
        public bool Contains(string value)
        {
            var node = _root;
            var valLen = value.Length;

            for (int i = 0; i < value.Length;) // i is incremented inside
            {
                // Try locking on next edge (if successful, this is already a match, hence the i++)
                if (!GetEdgeFor(node, value[i++], out node))
                    return false;

                // Match chars on locked edge until the end of edge or the end of value
                var edgeEnd = node.IsLeaf ? _position + 1 : node.End;
                for (int j = node.Start + 1; j < edgeEnd && i < valLen; j++, i++)
                    if (_chars[j] != value[i])
                        return false;
            }

            return true;
        }

        /// <summary>
        /// Creates a string representation of the tree in a rather primitive way.
        /// Works only with tree content consisting of lowercase a-z characters.
        /// Perhaps useful for debugging.
        /// </summary>
        public string PrintTree()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Content length: {_chars.Count}{Environment.NewLine}");
            Print(0, _root);
            return sb.ToString();

            void Print(int depth, Node node)
            {
                var activeOrigin = "";
                var nodeLabel = LabelOf(node);
                var openEndMark = "";
                var linkMark = "";

                if (node == _AP.ActiveParent)
                    activeOrigin = ">";

                if (node == _AP.ActiveEdge)
                    nodeLabel = nodeLabel.Insert(_AP.ActiveLength, " | ");

                if (node.IsLeaf)
                    openEndMark = "...";

                if (GetLinkFor(node, out var linkedNode))
                    linkMark = " -> " + FirstCharOf(linkedNode);

                sb.AppendLine(new string(' ', depth + 1 - activeOrigin.Length) + activeOrigin + depth + ":" + nodeLabel + openEndMark + linkMark);

                for (char c = 'A'; c <= 'z'; c++)
                {
                    if (_structure.TryGetValue((node, c), out var childNode))
                        Print(depth + 1, childNode);
                }
            }
        }

        string wynik = "";
        public void MyFunction()
        {
            List<string> helper = new List<string>();

            string max = String.Empty;
            Test(0, _root);

            if (wynik.Length < 1) Console.WriteLine("Nic się wtym ciagu nie powtarza oprócz pojedynczych znakow");
            else Console.WriteLine("Największym powtarzajacym się prefixem jest (" + wynik + ")");
            Console.ReadLine();
            return;

            void Test(int depth, Node node)
            {
                if (depth + 1 > helper.Count) helper.Add("");

                if (node.IsLeaf) return; //jak do nikąd nie idzie to cofaj

                int numberOfNodes = 0;
                if (node != _AP.ActiveParent) numberOfNodes++;

                var nodeLabel = LabelOf(node);
                if (node == _AP.ActiveEdge)
                    nodeLabel = nodeLabel.Insert(_AP.ActiveLength, " | "); //nodeLabel jest tekst



                for (char c = 'A'; c <= 'z'; c++)
                {
                    if (_structure.TryGetValue((node, c), out var childNode))
                    {
                        helper[depth] = nodeLabel;
                        Test(depth + 1, childNode);
                        numberOfNodes++;

                    }
                }
                string temp = String.Empty;
                for (int i = 0; i < helper.Count; i++) temp += helper[i];
                if (numberOfNodes >= 1 && temp.Length > wynik.Length) wynik = temp;
                helper.RemoveAt(depth);
            }

        }

        public int znajdzK(string tekst)
        {

            return znajdzKWewnetrzna(tekst, "", 0, _root);

            int znajdzKWewnetrzna(string txt, string powtorzenie, int wynik, Node node)
            {

                if (!_structure.TryGetValue((node, txt[0]), out var childNode))
                    return wynik;

                string temp = LabelOf(childNode);
                string powtorzenie2 = "";
                string txt2 = "";
                if (temp.Length > 1)
                {
                    powtorzenie2 = powtorzenie + temp[0] + temp[1];
                    txt2 = txt.Remove(0, 2);
                }
                powtorzenie += temp;
                txt = txt.Remove(0, temp.Length);




                if (powtorzenie.Length > txt.Length)
                    return wynik;

                wynik = znajdzKWewnetrzna(txt, powtorzenie, wynik, childNode);

                if (wynik == 0 && temp.Length > 1)
                {
                    for (int j = 0; j < powtorzenie2.Length; j++)
                    {
                        if (powtorzenie2[j] == txt2[j])
                        {
                            if (j + 1 == powtorzenie2.Length) { wynik = powtorzenie2.Length; break; }
                        }
                        else break;
                    }
                }

                if (wynik == 0)
                {
                    for (int j = 0; j < powtorzenie.Length; j++)
                    {
                        if (powtorzenie[j] == txt[j])
                        {
                            if (j + 1 == powtorzenie.Length) { wynik = powtorzenie.Length; break; }
                        }
                        else break;
                    }
                }


                return wynik;
            }
        }

        public void writeLine(string tekst)
        {
            Console.WriteLine("Dla podanego ciagu K wynosi " + tekst);
        }
    }
}



//// Check against infinite edge match
// if (AP.ActiveEdge != null && AP.ActiveEdge.Start + AP.ActiveLength == _position + 1) throw new InvalidOperationException("Infinite match error.");

//// Old Rescan method
//public void Rescan()
//{
//    //ActiveParent = ActiveParent.Link ?? _tree._root;
//    //ActiveParent = _tree._suffixLinks.TryGetValue(ActiveParent, out var linkedNode) ? linkedNode : _tree._root;
//    //var suffixLength = ActiveParent == _tree._root ? _tree._remainder - 1 : ActiveLength;

//    var suffixLength = 0;
//    if (_tree._suffixLinks.TryGetValue(ActiveParent, out var linkedNode))
//    {
//        ActiveParent = linkedNode;
//        suffixLength = ActiveLength;
//    }
//    else
//    {
//        ResetEdge(); // Existing edge is incompatible with root
//        ActiveParent = _tree._root;
//        suffixLength = _tree._remainder - 1;
//    }

//    while (suffixLength > 0)
//    {
//        ActiveEdge = _tree._structure[(ActiveParent, _tree._chars[_tree._position - suffixLength])];
//        var edgeLength = _tree.LengthOf(ActiveEdge);

//        if (edgeLength > suffixLength)
//        {
//            ActiveLength = suffixLength;
//            break;
//        }

//        suffixLength -= edgeLength;
//        ActiveParent = ActiveEdge;
//        ResetEdge();
//    }
//}