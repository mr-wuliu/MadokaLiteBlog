public class PostListState
{
    public List<Post> Posts { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 5;
    public bool HasData { get; set; }
    
    public bool HasNextPage => Posts.Count >= PageSize;
    public bool HasPreviousPage => CurrentPage > 1;
    
    public event Action? OnStateChanged;
    
    public void NotifyStateChanged() => OnStateChanged?.Invoke();
    
    public void Clear()
    {
        Posts.Clear();
        CurrentPage = 1;
        HasData = false;
        NotifyStateChanged();
    }
} 