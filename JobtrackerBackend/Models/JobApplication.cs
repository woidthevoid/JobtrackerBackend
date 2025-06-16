using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace JobtrackerBackend;
using Supabase;
[Table("job_applications")]
public class JobApplication : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }
    
    [Column("user_id")]
    public Guid UserId { get; set; }
    
    [Column("title")]
    public string Title { get; set; }
    
    [Column("description")]
    public string Description { get; set; }
    
    [Column("job_link")]
    public string JobLink { get; set; }
    
    [Column("cv_file_url")]
    public string CvFileUrl { get; set; }
    
    [Column("app_file_url")]
    public string AppFileUrl { get; set; }
    
    [Column("application_status")]
    public string ApplicationStatus { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}