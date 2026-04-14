using System;
using System.Collections.Generic;
using System.Text;

namespace MOD.Training.Training.Finance.Dtos
{
    /// <summary>
    /// Sub-item (ParentId != null) with parent name for UI grouping.
    /// </summary>
    public class FinancialItemSubItemDto
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = null!;
        public string NameEn { get; set; } = null!;
        public string Code { get; set; } = null!;
        public Guid ParentId { get; set; }

        /// <summary>Parent item's Arabic name — used for dx-select-box grouping.</summary>
        public string ParentNameAr { get; set; } = null!;

        /// <summary>Parent item's English name — used for dx-select-box grouping.</summary>
        public string ParentNameEn { get; set; } = null!;
    }
}
