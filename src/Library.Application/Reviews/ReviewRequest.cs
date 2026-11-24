namespace Library.Application.Reviews;

public sealed record ReviewRequest(int Rating, string Comment = "");
