namespace KATCRUDServices.Core.Models
{
    /// <summary>
    /// Sample Leave Request model with auto-number support
    /// </summary>
    public class LeaveRequest
    {
        /// <summary>
        /// Unique identifier for the leave request (auto-generated)
        /// </summary>
        public int LeaveRequestId { get; set; }

        /// <summary>
        /// Leave Request Number - auto-generated with pattern (e.g., LR-00001-23112025)
        /// </summary>
        public string LeaveRequestNumber { get; set; } = string.Empty;

        /// <summary>
        /// Employee ID
        /// </summary>
        public int EmployeeId { get; set; }

        /// <summary>
        /// Employee Name
        /// </summary>
        public string EmployeeName { get; set; } = string.Empty;

        /// <summary>
        /// Leave Type (e.g., Casual, Sick, Annual)
        /// </summary>
        public string LeaveType { get; set; } = string.Empty;

        /// <summary>
        /// Start Date of leave
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End Date of leave
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Number of days
        /// </summary>
        public int NumberOfDays { get; set; }

        /// <summary>
        /// Reason for leave
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Status (Pending, Approved, Rejected)
        /// </summary>
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Approver ID
        /// </summary>
        public int? ApproverId { get; set; }

        /// <summary>
        /// Approver Comments
        /// </summary>
        public string? ApproverComments { get; set; }

        /// <summary>
        /// Created Date
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Modified Date
        /// </summary>
        public DateTime? ModifiedDate { get; set; }

        /// <summary>
        /// Is Active
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
