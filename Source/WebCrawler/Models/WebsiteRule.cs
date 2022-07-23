using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebCrawler.Models
{
    [Table("atc_websiterules")]
    public class WebsiteRule
    {
        [Column("ruleid")]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid RuleId { get; set; }

        [Column("type", TypeName = "varchar")]
        public WebsiteRuleType Type { get; set; }

        [Column("websiteid")]
        public int WebsiteId { get; set; }

        #region Page Load

        [Column("pg_loadoption", TypeName = "varchar")]
        public PageLoadOption PageLoadOption { get; set; }

        [Column("pg_exp_urlrevise")]
        public string? PageUrlReviseExp { get; set; }

        [Column("pg_exp_urlreplacement")]
        public string? PageUrlReplacement { get; set; }

        #endregion

        #region Content Match

        [Column("cnt_matchtype", TypeName = "varchar")]
        public ContentMatchType ContentMatchType { get; set; }

        [Column("cnt_exp_root")]
        public string? ContentRootExp { get; set; }

        [Column("cnt_exp_url")]
        public string? ContentUrlExp { get; set; }

        [Column("cnt_exp_urlrevise")]
        public string? ContentUrlReviseExp { get; set; }

        [Column("cnt_exp_urlreplacement")]
        public string? ContentUrlReplacement { get; set; }

        [Column("cnt_exp_title")]
        public string? ContentTitleExp { get; set; }

        [Column("cnt_exp_date")]
        public string? ContentDateExp { get; set; }

        [Column("cnt_exp_content")]
        public string? ContentExp { get; set; }

        #endregion


        // NOTES: use Fluent API instead as the ForeignKey attribute doesn't appear working
        //[ForeignKey("websiteid")]
        public Website Website { get; set; }
    }
}
