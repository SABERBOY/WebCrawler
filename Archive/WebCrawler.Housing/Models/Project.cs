using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebCrawler.Housing.Models
{
    [Table("housing_projects")]
    public class Project
    {
        [Key]
        public int Id { get; set; }
        public string TownCode { get; set; }
        [Display(Name = "项目名称")]
        public string Name { get; set; }
        [Display(Name = "主要用途")]
        public string Purpose { get; set; }
        [Display(Name = "楼盘地址")]
        public string Location { get; set; }
        [Display(Name = "开发单位")]
        public string Developer { get; set; }
        [Display(Name = "法人代表")]
        public string LegalRepresentative { get; set; }
        [Display(Name = "预（销）售许可证")]
        public string PresellLicenseNo { get; set; }
        [Display(Name = "建设用地规划许可证")]
        public string LandLicenseNo { get; set; }
        [Display(Name = "建设工程规划许可证")]
        public string PlanningLicenseNo { get; set; }
        [Display(Name = "建设工程施工许可证")]
        public string ConstructionLicenseNo { get; set; }
        [Display(Name = "施工单位")]
        public string ConstructionCompany { get; set; }
        [Display(Name = "确权情况")]
        public string PropertyRight { get; set; }
        [Display(Name = "总建筑面积")]
        public double TotalArea { get; set; }
        [Display(Name = "总套数")]
        public int TotalCount { get; set; }
        [Display(Name = "可销售面积")]
        public double SellableArea { get; set; }
        [Display(Name = "可销售套数")]
        public int SellableCount { get; set; }
        [Display(Name = "已销售面积")]
        public double SoldArea { get; set; }
        [Display(Name = "已销售套数")]
        public int SoldCount { get; set; }
        [Display(Name = "不可销售面积")]
        public double UnsellableArea { get; set; }
        [Display(Name = "不可销售套数")]
        public int UnsellableCount { get; set; }
        [Display(Name = "备注")]
        public string Notes { get; set; }
        public string URL { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
