using System.Security.Claims;
using InsightFlow.API.Data;
using InsightFlow.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InsightFlow.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DailyRecordsController : ControllerBase
    {
        private readonly InsightFlowDbContext _context;

        public DailyRecordsController(InsightFlowDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // CREATE DAILY RECORD
        // POST: api/DailyRecords
        //
        // EmployeeAccountId is NOT supplied by the user.
        // It is taken from the authenticated JWT.
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> CreateRecord(
            [FromBody] CreateDailyRecordRequest request)
        {
            long? accountId = GetCurrentAccountId();

            if (accountId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid authentication token."
                });
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e =>
                    e.AccountId == accountId.Value);

            if (employee == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Employee account could not be found."
                });
            }

            if (!employee.IsActive)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "This employee account is disabled."
                });
            }

            // -------------------------------------------------
            // Validate date
            // -------------------------------------------------
            if (request.RecordDate == default)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Record date is required."
                });
            }

            if (request.RecordDate.Date > DateTime.UtcNow.Date)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "A daily record cannot be submitted for a future date."
                });
            }

            // -------------------------------------------------
            // Validate required fields
            // -------------------------------------------------
            if (string.IsNullOrWhiteSpace(request.ActivityType))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Activity type is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Status is required."
                });
            }

            if (request.Quantity < 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Quantity cannot be negative."
                });
            }

            if (request.BusinessValue < 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Business value cannot be negative."
                });
            }

            if (request.SecondaryMetric < 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Secondary metric cannot be negative."
                });
            }

            // -------------------------------------------------
            // Create record
            // -------------------------------------------------
            var record = new DailyRecord
            {
                EmployeeAccountId = employee.AccountId,

                RecordDate = request.RecordDate.Date,

                ActivityType =
                    request.ActivityType.Trim(),

                Reference =
                    request.Reference?.Trim()
                    ?? string.Empty,

                Quantity = request.Quantity,

                BusinessValue = request.BusinessValue,

                SecondaryMetric = request.SecondaryMetric,

                Status =
                    request.Status.Trim(),

                BusinessImpact =
                    request.BusinessImpact?.Trim()
                    ?? string.Empty,

                Notes =
                    request.Notes?.Trim()
                    ?? string.Empty,

                SubmittedAt = DateTime.UtcNow
            };

            _context.DailyRecords.Add(record);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetRecord),
                new
                {
                    recordId = record.RecordId
                },
                new
                {
                    success = true,
                    message =
                        "Daily record submitted successfully.",

                    record = new
                    {
                        recordId = record.RecordId,

                        employee = new
                        {
                            accountId = employee.AccountId,
                            employeeId = employee.EmployeeId,

                            fullName =
                                $"{employee.FirstName} {employee.LastName}",

                            department =
                                employee.Department?.DepartmentName,

                            departmentCode =
                                employee.Department?.DepartmentCode
                        },

                        recordDate = record.RecordDate,
                        activityType = record.ActivityType,
                        reference = record.Reference,
                        quantity = record.Quantity,
                        businessValue = record.BusinessValue,
                        secondaryMetric = record.SecondaryMetric,
                        status = record.Status,
                        businessImpact = record.BusinessImpact,
                        notes = record.Notes,
                        submittedAt = record.SubmittedAt
                    }
                });
        }

        // =====================================================
        // GET MY DAILY RECORDS
        // GET: api/DailyRecords/my
        // =====================================================
        [HttpGet("my")]
        public async Task<IActionResult> GetMyRecords()
        {
            long? accountId = GetCurrentAccountId();

            if (accountId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid authentication token."
                });
            }

            var records = await _context.DailyRecords
                .AsNoTracking()
                .Where(r =>
                    r.EmployeeAccountId == accountId.Value)
                .OrderByDescending(r => r.RecordDate)
                .ThenByDescending(r => r.SubmittedAt)
                .Select(r => new
                {
                    recordId = r.RecordId,
                    recordDate = r.RecordDate,
                    activityType = r.ActivityType,
                    reference = r.Reference,
                    quantity = r.Quantity,
                    businessValue = r.BusinessValue,
                    secondaryMetric = r.SecondaryMetric,
                    status = r.Status,
                    businessImpact = r.BusinessImpact,
                    notes = r.Notes,
                    submittedAt = r.SubmittedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                count = records.Count,
                records
            });
        }

        // =====================================================
        // GET ALL DAILY RECORDS
        // GET: api/DailyRecords
        //
        // Administrator and Manager only
        // =====================================================
        [Authorize(Roles = "Administrator,Manager")]
        [HttpGet]
        public async Task<IActionResult> GetAllRecords()
        {
            var records = await _context.DailyRecords
                .AsNoTracking()
                .Include(r => r.Employee)
                .ThenInclude(e => e.Department)
                .OrderByDescending(r => r.RecordDate)
                .ThenByDescending(r => r.SubmittedAt)
                .Select(r => new
                {
                    recordId = r.RecordId,

                    employee = new
                    {
                        accountId =
                            r.EmployeeAccountId,

                        employeeId =
                            r.Employee != null
                                ? r.Employee.EmployeeId
                                : string.Empty,

                        firstName =
                            r.Employee != null
                                ? r.Employee.FirstName
                                : string.Empty,

                        lastName =
                            r.Employee != null
                                ? r.Employee.LastName
                                : string.Empty,

                        department =
                            r.Employee != null &&
                            r.Employee.Department != null
                                ? r.Employee.Department.DepartmentName
                                : string.Empty,

                        departmentCode =
                            r.Employee != null &&
                            r.Employee.Department != null
                                ? r.Employee.Department.DepartmentCode
                                : string.Empty
                    },

                    recordDate = r.RecordDate,
                    activityType = r.ActivityType,
                    reference = r.Reference,
                    quantity = r.Quantity,
                    businessValue = r.BusinessValue,
                    secondaryMetric = r.SecondaryMetric,
                    status = r.Status,
                    businessImpact = r.BusinessImpact,
                    notes = r.Notes,
                    submittedAt = r.SubmittedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                count = records.Count,
                records
            });
        }

        // =====================================================
        // GET ONE DAILY RECORD
        // GET: api/DailyRecords/{recordId}
        //
        // Employee may see their own record.
        // Administrator / Manager may see any record.
        // =====================================================
        [HttpGet("{recordId:long}")]
        public async Task<IActionResult> GetRecord(long recordId)
        {
            long? accountId = GetCurrentAccountId();

            if (accountId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid authentication token."
                });
            }

            var record = await _context.DailyRecords
                .AsNoTracking()
                .Include(r => r.Employee)
                .ThenInclude(e => e.Department)
                .FirstOrDefaultAsync(r =>
                    r.RecordId == recordId);

            if (record == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Daily record was not found."
                });
            }

            if (!CanAccessRecord(
                    record.EmployeeAccountId,
                    accountId.Value))
            {
                return Forbid();
            }

            return Ok(new
            {
                success = true,

                record = new
                {
                    recordId = record.RecordId,

                    employee = new
                    {
                        accountId =
                            record.EmployeeAccountId,

                        employeeId =
                            record.Employee?.EmployeeId,

                        fullName =
                            record.Employee == null
                                ? string.Empty
                                : $"{record.Employee.FirstName} " +
                                  $"{record.Employee.LastName}",

                        department =
                            record.Employee?.Department?.DepartmentName,

                        departmentCode =
                            record.Employee?.Department?.DepartmentCode
                    },

                    recordDate = record.RecordDate,
                    activityType = record.ActivityType,
                    reference = record.Reference,
                    quantity = record.Quantity,
                    businessValue = record.BusinessValue,
                    secondaryMetric = record.SecondaryMetric,
                    status = record.Status,
                    businessImpact = record.BusinessImpact,
                    notes = record.Notes,
                    submittedAt = record.SubmittedAt
                }
            });
        }

        // =====================================================
        // UPDATE DAILY RECORD
        // PUT: api/DailyRecords/{recordId}
        //
        // Employee may update their own record.
        // Administrator / Manager may update any record.
        // =====================================================
        [HttpPut("{recordId:long}")]
        public async Task<IActionResult> UpdateRecord(
            long recordId,
            [FromBody] UpdateDailyRecordRequest request)
        {
            long? accountId = GetCurrentAccountId();

            if (accountId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid authentication token."
                });
            }

            var record = await _context.DailyRecords
                .FirstOrDefaultAsync(r =>
                    r.RecordId == recordId);

            if (record == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Daily record was not found."
                });
            }

            if (!CanAccessRecord(
                    record.EmployeeAccountId,
                    accountId.Value))
            {
                return Forbid();
            }

            // -------------------------------------------------
            // Validation
            // -------------------------------------------------
            if (request.RecordDate == default)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Record date is required."
                });
            }

            if (request.RecordDate.Date > DateTime.UtcNow.Date)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "A daily record cannot use a future date."
                });
            }

            if (string.IsNullOrWhiteSpace(request.ActivityType))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Activity type is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Status is required."
                });
            }

            if (request.Quantity < 0 ||
                request.BusinessValue < 0 ||
                request.SecondaryMetric < 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Quantity, business value and secondary " +
                        "metric cannot be negative."
                });
            }

            // -------------------------------------------------
            // Update
            // -------------------------------------------------
            record.RecordDate =
                request.RecordDate.Date;

            record.ActivityType =
                request.ActivityType.Trim();

            record.Reference =
                request.Reference?.Trim()
                ?? string.Empty;

            record.Quantity =
                request.Quantity;

            record.BusinessValue =
                request.BusinessValue;

            record.SecondaryMetric =
                request.SecondaryMetric;

            record.Status =
                request.Status.Trim();

            record.BusinessImpact =
                request.BusinessImpact?.Trim()
                ?? string.Empty;

            record.Notes =
                request.Notes?.Trim()
                ?? string.Empty;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message =
                    "Daily record updated successfully.",

                record = new
                {
                    recordId = record.RecordId,
                    recordDate = record.RecordDate,
                    activityType = record.ActivityType,
                    reference = record.Reference,
                    quantity = record.Quantity,
                    businessValue = record.BusinessValue,
                    secondaryMetric = record.SecondaryMetric,
                    status = record.Status,
                    businessImpact = record.BusinessImpact,
                    notes = record.Notes,
                    submittedAt = record.SubmittedAt
                }
            });
        }

        // =====================================================
        // DELETE DAILY RECORD
        // DELETE: api/DailyRecords/{recordId}
        //
        // Administrator only.
        // We keep deletion restricted because these records
        // will be used for reporting and analytics.
        // =====================================================
        [Authorize(Roles = "Administrator")]
        [HttpDelete("{recordId:long}")]
        public async Task<IActionResult> DeleteRecord(
            long recordId)
        {
            var record = await _context.DailyRecords
                .FirstOrDefaultAsync(r =>
                    r.RecordId == recordId);

            if (record == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Daily record was not found."
                });
            }

            _context.DailyRecords.Remove(record);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message =
                    "Daily record deleted successfully."
            });
        }

        // =====================================================
        // GET CURRENT ACCOUNT ID FROM JWT
        // =====================================================
        private long? GetCurrentAccountId()
        {
            string? accountIdValue =
                User.FindFirst(
                    ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(accountIdValue))
            {
                return null;
            }

            if (!long.TryParse(
                    accountIdValue,
                    out long accountId))
            {
                return null;
            }

            return accountId;
        }

        // =====================================================
        // CHECK RECORD ACCESS
        // =====================================================
        private bool CanAccessRecord(
            long recordEmployeeAccountId,
            long currentAccountId)
        {
            // Employee owns the record
            if (recordEmployeeAccountId == currentAccountId)
            {
                return true;
            }

            // Administrator / Manager may access other records
            if (User.IsInRole("Administrator") ||
                User.IsInRole("Manager"))
            {
                return true;
            }

            return false;
        }
    }

    // =========================================================
    // CREATE DAILY RECORD REQUEST
    // =========================================================
    public class CreateDailyRecordRequest
    {
        public DateTime RecordDate { get; set; }

        public string ActivityType { get; set; }
            = string.Empty;

        public string Reference { get; set; }
            = string.Empty;

        public int Quantity { get; set; }

        public decimal BusinessValue { get; set; }

        public int SecondaryMetric { get; set; }

        public string Status { get; set; }
            = string.Empty;

        public string BusinessImpact { get; set; }
            = string.Empty;

        public string Notes { get; set; }
            = string.Empty;
    }

    // =========================================================
    // UPDATE DAILY RECORD REQUEST
    // =========================================================
    public class UpdateDailyRecordRequest
    {
        public DateTime RecordDate { get; set; }

        public string ActivityType { get; set; }
            = string.Empty;

        public string Reference { get; set; }
            = string.Empty;

        public int Quantity { get; set; }

        public decimal BusinessValue { get; set; }

        public int SecondaryMetric { get; set; }

        public string Status { get; set; }
            = string.Empty;

        public string BusinessImpact { get; set; }
            = string.Empty;

        public string Notes { get; set; }
            = string.Empty;
    }
}