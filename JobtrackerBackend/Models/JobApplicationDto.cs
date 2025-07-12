namespace JobtrackerBackend;

public class JobApplicationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string JobLink { get; set; }
    public string CvFileUrl { get; set; }
    public string AppFileUrl { get; set; }
    public string ApplicationStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}