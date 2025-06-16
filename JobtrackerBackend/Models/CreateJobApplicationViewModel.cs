namespace JobtrackerBackend;

public class CreateJobApplicationViewModel
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string JobLink { get; set; }
    public string ApplicationStatus { get; set; }
    
    public IFormFile? CvFile { get; set; }
    public IFormFile? AppFile { get; set; }
}