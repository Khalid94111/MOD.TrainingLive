using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MOD.Training.Shared
{
    /// <summary>
    /// Wrapper DTO for batch sort order update.
    /// </summary>
    public class UpdateSortOrderInput
    {
        [Required]
        public List<SortOrderItem> Items { get; set; } = [];
    }

    /// <summary>
    /// Single item in the sort order batch update.
    /// </summary>
    public class SortOrderItem
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public int SortOrder { get; set; }
    }

}
