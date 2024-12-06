using System.Text;

namespace Serializer
{
    //Specify your class\file name and complete implementation.
    public class ListSerializer : IListSerializer
    {
        //the constructor with no parameters is required and no other constructors can be used.
        public ListSerializer()
        {
            //...
        }

        public Task<ListNode> DeepCopy(ListNode head)
        {
            ArgumentNullException.ThrowIfNull(head);
            
            var nodeMap = new Dictionary<ListNode, ListNode>();
            
            for (var current = head; current != null; current = current.Next)
            {
                nodeMap[current] = new ListNode { Data = current.Data };
            }
            
            foreach (var originalNode in nodeMap.Keys)
            {
                var copiedNode = nodeMap[originalNode];
                
                copiedNode.Previous = originalNode.Previous != null ? nodeMap[originalNode.Previous] : null;
                copiedNode.Next = originalNode.Next != null ? nodeMap[originalNode.Next] : null;
                copiedNode.Random = originalNode.Random != null ? nodeMap[originalNode.Random] : null;
            }

            return Task.FromResult(nodeMap[head]);
        }

        public async Task<ListNode> Deserialize(Stream s)
        {
            ArgumentNullException.ThrowIfNull(s);
            if (s.Length == 0) throw new ArgumentException("Stream contains no data.");

            using var reader = new StreamReader(s, Encoding.UTF8);
            var idToNode = new Dictionary<int, ListNode>();
            var pendingLinks = new List<(int id, int prevId, int nextId, int randomId)>();
            ListNode? headNode = null;

            for (var id = 0; !reader.EndOfStream; id++)
            {
                var line = await reader.ReadLineAsync();
                
                var parts = line.Split(',');
                if (parts.Length != 4) throw new ArgumentException($"Invalid data format: '{line}'");

                if (!int.TryParse(parts[0], out var prevId))
                    throw new ArgumentException($"Invalid value for prevId: '{parts[0]}'");
                if (!int.TryParse(parts[1], out var nextId))
                    throw new ArgumentException($"Invalid value for nextId: '{parts[1]}'");
                if (!int.TryParse(parts[2], out var randomId))
                    throw new ArgumentException($"Invalid value for randomId: '{parts[2]}'");
                var data = parts[3];

                var node = new ListNode { Data = data };
                idToNode[id] = node;

                pendingLinks.Add((id, prevId, nextId, randomId));

                if (prevId == -1)
                    headNode = node;
            }

            if (headNode == null) throw new ArgumentException("Stream data does not contain a valid head node.");
            
            foreach (var (currentId, prevId, nextId, randomId) in pendingLinks)
            {
                var currentNode = idToNode[currentId];
        
                currentNode.Previous = prevId != -1 ? idToNode[prevId] : null;
                currentNode.Next = nextId != -1 ? idToNode[nextId] : null;
                currentNode.Random = randomId != -1 ? idToNode[randomId] : null;
            }

            return headNode;
        }

        public async Task Serialize(ListNode head, Stream s)
        {
            ArgumentNullException.ThrowIfNull(head);
            ArgumentNullException.ThrowIfNull(s);
            
            await using var writer = new StreamWriter(s, Encoding.UTF8, leaveOpen: true);
            var nodeToId = new Dictionary<ListNode, int>();
            var nodes = new List<ListNode>();
            var id = 0;
            
            for (var current = head; current != null; current = current.Next, id++)
            {
                nodeToId[current] = id;
                nodes.Add(current);
            }

            foreach (var node in nodes)
            {
                var data = node.Data ?? string.Empty;
                var prevId = node.Previous != null ? nodeToId[node.Previous] : -1;
                var nextId = node.Next != null ? nodeToId[node.Next] : -1;
                var randomId = node.Random != null ? nodeToId[node.Random] : -1;

                var line = $"{prevId},{nextId},{randomId},{data}";
                await writer.WriteLineAsync(line);
            }
        }
    }
}
