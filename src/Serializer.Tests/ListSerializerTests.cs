using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Serializer.Tests;

public class ListSerializerTests
{
	private readonly IListSerializer _listSerializer;

	public ListSerializerTests()
	{
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IListSerializer, ListSerializer>()
            .BuildServiceProvider();

        _listSerializer = serviceProvider.GetRequiredService<IListSerializer>();
	}
	
    [Fact]
    public async Task DeepCopy_ShouldCreateIndependentCopy()
    {
        // Arrange
        var originalHead = CreateTestList();
        
        // Act
        var copyHead = await _listSerializer.DeepCopy(originalHead);

        // Assert
        Assert.NotSame(originalHead, copyHead);
        Assert.NotSame(originalHead.Next, copyHead.Next);
        Assert.Equal(originalHead.Data, copyHead.Data);
        Assert.NotSame(originalHead.Random, copyHead.Random);
    }

    [Fact]
    public async Task Deserialize_ShouldReturnCorrectList()
    {
        // Arrange
        const string serializedData = "-1,1,2,Data1\n0,2,-1,Data2\n1,-1,1,Data3\n";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(serializedData));

        // Act
        var deserializedHead = await _listSerializer.Deserialize(stream);

        // Assert
        Assert.NotNull(deserializedHead);
        Assert.Equal("Data1", deserializedHead.Data);
        Assert.Equal("Data2", deserializedHead.Next.Data);
        Assert.Equal("Data3", deserializedHead.Next.Next.Data);
        Assert.Null(deserializedHead.Previous);
    }

    [Fact]
    public async Task Serialize_ShouldWriteCorrectDataToStream()
    {
        // Arrange
        var head = CreateTestList();
        var stream = new MemoryStream();

        // Act
        await _listSerializer.Serialize(head, stream);
        stream.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(stream);
        var serializedData = await reader.ReadToEndAsync();
        
        // Assert
        Assert.Contains("-1,1,2,Data1", serializedData);
        Assert.Contains("0,2,-1,Data2", serializedData);
        Assert.Contains("1,-1,1,Data3", serializedData);
    }
    
    [Fact]
    public async Task SerializeDeserialize_ShouldHandleSingleElementList()
    {
        // Arrange
        var node = new ListNode { Data = "SingleNode" };
        var stream = new MemoryStream();

        // Act
        await _listSerializer.Serialize(node, stream);
        stream.Seek(0, SeekOrigin.Begin);
        var deserializedNode = await _listSerializer.Deserialize(stream);

        // Assert
        Assert.NotNull(deserializedNode);
        Assert.Equal("SingleNode", deserializedNode.Data);
        Assert.Null(deserializedNode.Next);
        Assert.Null(deserializedNode.Previous);
        Assert.Null(deserializedNode.Random);
    }

    [Fact]
    public async Task Deserialize_ShouldThrowArgumentException_WhenInvalidDataFormat()
    {
        // Arrange
        const string invalidData = "invalid,data,format";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidData));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _listSerializer.Deserialize(stream));
        Assert.Equal("Invalid data format: 'invalid,data,format'", exception.Message);
    }

    [Fact]
    public async Task Deserialize_ShouldThrowArgumentException_WhenMissingHeadNode()
    {
        // Arrange
        const string missingHeadData = "1,2,-1,Data1\n0,2,-1,Data2";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(missingHeadData));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _listSerializer.Deserialize(stream));
        Assert.Equal("Stream data does not contain a valid head node.", exception.Message);
    }
    
    [Fact]
    public async Task Deserialize_ShouldThrowArgumentException_WhenStreamIsEmpty()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _listSerializer.Deserialize(stream));
        Assert.Equal("Stream contains no data.", exception.Message);
    }

    [Fact]
    public async Task Serialize_ShouldThrowArgumentNullException_WhenListIsEmpty()
    {
        // Arrange
        ListNode? head = null;
        var stream = new MemoryStream();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _listSerializer.Serialize(head, stream));
        Assert.Equal("Value cannot be null. (Parameter 'head')", exception.Message);
    }

    private static ListNode CreateTestList()
    {
        var node1 = new ListNode { Data = "Data1" };
        var node2 = new ListNode { Data = "Data2" };
        var node3 = new ListNode { Data = "Data3" };

        node1.Next = node2;
        node2.Previous = node1;
        node2.Next = node3;
        node3.Previous = node2;
        
        node1.Random = node3;
        node3.Random = node2;

        return node1;
    }
}