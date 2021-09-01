using System;
using FreeSql.DataAnnotations;

namespace App
{
    [Table(Name = "t_area_base")]
    public class AreaBase
    {
        [Column(IsPrimary = true, Name = "FId")]
        public Guid Id { get; set; }

        [Column(Name = "FPId")]
        public Guid ParentId { get; set; }

        [Column(Name = "FGrade", MapType = typeof(string))]
        public GradeEnum Grade { get; set; }


        [Column(Name = "FPGrade", MapType = typeof(string))]
        public GradeEnum ParentGrade { get; set; }

        [Column(Name = "FCode")]
        public string Code { get; set; }

        [Column(Name = "FName")]
        public string Name { get; set; }

        [Column(Name = "FChildUrl")]
        public string ChildUrl { get; set; }
    }
    

    public enum GradeEnum
    {
        None = 0,
        Province = 1,
        City     = 2,
        County   = 3,
        Town     = 4,
        Village  = 5,
    }
}