using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

// Resolve .NET MAUI / QuestPDF naming conflicts
using IContainer = QuestPDF.Infrastructure.IContainer;
using Colors = QuestPDF.Helpers.Colors;

namespace InsightFlow.Services
{
    public class ReportPdfService
    {
        public ReportPdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // =========================================================
        // GENERATE REPORT
        // =========================================================

        public byte[] GenerateReport(
            string reportTitle,
            string reportPeriod,
            string filterSummary,
            List<ReportDailyRecordDto> records)
        {
            records ??= new List<ReportDailyRecordDto>();

            List<ReportDailyRecordDto> orderedRecords =
                records
                    .OrderBy(record => record.RecordDate)
                    .ThenBy(record => record.SubmittedAt)
                    .ToList();

            ReportStatistics statistics =
                CalculateStatistics(orderedRecords);

            List<ReportChartItem> departmentData =
                BuildDepartmentData(orderedRecords);

            List<ReportTrendItem> trendData =
                BuildTrendData(orderedRecords);

            return Document
                .Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);

                        page.Margin(28);

                        page.DefaultTextStyle(
                            style =>
                                style
                                    .FontSize(9)
                                    .FontColor("#0F172A"));

                        page.Header()
                            .Element(header =>
                                ComposeHeader(
                                    header,
                                    reportTitle,
                                    reportPeriod,
                                    filterSummary));

                        page.Content()
                            .PaddingVertical(15)
                            .Column(column =>
                            {
                                column.Spacing(14);

                                column.Item()
                                    .Element(content =>
                                        ComposeKpis(
                                            content,
                                            statistics));

                                column.Item()
                                    .Element(content =>
                                        ComposeBarChart(
                                            content,
                                            departmentData));

                                column.Item()
                                    .Element(content =>
                                        ComposePieChart(
                                            content,
                                            departmentData));

                                column.Item()
                                    .Element(content =>
                                        ComposeLineChart(
                                            content,
                                            trendData));

                                column.Item()
                                    .Element(content =>
                                        ComposeDepartmentBreakdown(
                                            content,
                                            departmentData));

                                column.Item()
                                    .Element(content =>
                                        ComposeRecords(
                                            content,
                                            orderedRecords));
                            });

                        page.Footer()
                            .Element(ComposeFooter);
                    });
                })
                .GeneratePdf();
        }

        // =========================================================
        // HEADER
        // =========================================================

        private static void ComposeHeader(
            IContainer container,
            string reportTitle,
            string reportPeriod,
            string filterSummary)
        {
            container
                .Background("#0F2747")
                .Padding(18)
                .Row(row =>
                {
                    row.RelativeItem()
                        .Column(column =>
                        {
                            column.Item()
                                .Text(
                                    string.IsNullOrWhiteSpace(reportTitle)
                                        ? "InsightFlow Performance Report"
                                        : reportTitle)
                                .FontSize(20)
                                .SemiBold()
                                .FontColor(Colors.White);

                            column.Item()
                                .PaddingTop(4)
                                .Text(
                                    string.IsNullOrWhiteSpace(reportPeriod)
                                        ? "Reporting period"
                                        : reportPeriod)
                                .FontSize(9)
                                .FontColor("#BFDBFE");

                            if (!string.IsNullOrWhiteSpace(filterSummary))
                            {
                                column.Item()
                                    .PaddingTop(3)
                                    .Text(filterSummary)
                                    .FontSize(8)
                                    .FontColor("#93C5FD");
                            }
                        });

                    row.ConstantItem(100)
                        .AlignRight()
                        .AlignMiddle()
                        .Text("INSIGHTFLOW")
                        .FontSize(11)
                        .Bold()
                        .FontColor("#93C5FD");
                });
        }

        // =========================================================
        // KPI SECTION
        // =========================================================

        private static void ComposeKpis(
            IContainer container,
            ReportStatistics statistics)
        {
            container.Column(column =>
            {
                column.Spacing(8);

                column.Item()
                    .Text("Performance Summary")
                    .FontSize(14)
                    .SemiBold();

                column.Item()
                    .Row(row =>
                    {
                        row.Spacing(8);

                        row.RelativeItem()
                            .Element(card =>
                                KpiCard(
                                    card,
                                    "ACTIVITIES",
                                    statistics.TotalActivities.ToString("N0")));

                        row.RelativeItem()
                            .Element(card =>
                                KpiCard(
                                    card,
                                    "COMPLETED",
                                    statistics.Completed.ToString("N0")));

                        row.RelativeItem()
                            .Element(card =>
                                KpiCard(
                                    card,
                                    "COMPLETION RATE",
                                    $"{statistics.CompletionRate:N1}%"));

                        row.RelativeItem()
                            .Element(card =>
                                KpiCard(
                                    card,
                                    "BUSINESS VALUE",
                                    $"R {statistics.BusinessValue:N2}"));
                    });

                column.Item()
                    .Row(row =>
                    {
                        row.Spacing(8);

                        row.RelativeItem()
                            .Element(card =>
                                KpiCard(
                                    card,
                                    "CONTRIBUTORS",
                                    statistics.Contributors.ToString("N0")));

                        row.RelativeItem()
                            .Element(card =>
                                KpiCard(
                                    card,
                                    "TOTAL QUANTITY",
                                    statistics.TotalQuantity.ToString("N0")));

                        row.RelativeItem()
                            .Element(card =>
                                KpiCard(
                                    card,
                                    "IN PROGRESS",
                                    statistics.InProgress.ToString("N0")));

                        row.RelativeItem()
                            .Element(card =>
                                KpiCard(
                                    card,
                                    "PENDING / HOLD",
                                    statistics.PendingHold.ToString("N0")));
                    });
            });
        }

        private static void KpiCard(
            IContainer container,
            string title,
            string value)
        {
            container
                .Border(1)
                .BorderColor("#E2E8F0")
                .Background("#F8FAFC")
                .Padding(10)
                .Column(column =>
                {
                    column.Item()
                        .Text(title)
                        .FontSize(7)
                        .SemiBold()
                        .FontColor("#64748B");

                    column.Item()
                        .PaddingTop(5)
                        .Text(value)
                        .FontSize(14)
                        .SemiBold()
                        .FontColor("#0F172A");
                });
        }

        // =========================================================
        // BAR CHART
        // =========================================================

        private static void ComposeBarChart(
            IContainer container,
            List<ReportChartItem> data)
        {
            container.Column(column =>
            {
                column.Spacing(8);

                column.Item()
                    .Text("Department Activity — Bar Chart")
                    .FontSize(14)
                    .SemiBold();

                if (data.Count == 0)
                {
                    column.Item()
                        .Text("No department activity is available.")
                        .FontColor("#64748B");

                    return;
                }

                int maximum =
                    Math.Max(
                        1,
                        data.Max(item => item.Activities));

                foreach (ReportChartItem item in data)
                {
                    column.Item()
                        .Row(row =>
                        {
                            row.ConstantItem(115)
                                .Text(item.Name)
                                .FontSize(8);

                            row.RelativeItem()
                                .Height(15)
                                .Layers(layers =>
                                {
                                    layers.Layer()
                                        .Background("#E2E8F0");

                                    layers.PrimaryLayer()
                                        .Width(
                                            Math.Max(
                                                1,
                                                item.Activities * 100f /
                                                maximum))
                                        .Background("#2563EB");
                                });

                            row.ConstantItem(35)
                                .AlignRight()
                                .Text(item.Activities.ToString("N0"))
                                .FontSize(8)
                                .SemiBold();
                        });
                }
            });
        }

        // =========================================================
        // PIE CHART
        // =========================================================

        private static void ComposePieChart(
            IContainer container,
            List<ReportChartItem> data)
        {
            container.Column(column =>
            {
                column.Spacing(8);

                column.Item()
                    .Text("Business Value by Department — Pie Chart")
                    .FontSize(14)
                    .SemiBold();

                List<ReportChartItem> pieData =
                    data
                        .Where(item => item.BusinessValue > 0)
                        .ToList();

                if (pieData.Count == 0)
                {
                    column.Item()
                        .Text(
                            "No positive business value is available for the pie chart.")
                        .FontColor("#64748B");

                    return;
                }

                column.Item()
                    .Row(row =>
                    {
                        row.ConstantItem(210)
                            .Height(190)
                            .Svg(size =>
                                BuildPieChartSvg(
                                    pieData,
                                    size.Width,
                                    size.Height));

                        row.RelativeItem()
                            .PaddingLeft(15)
                            .Column(legend =>
                            {
                                legend.Spacing(7);

                                string[] colors =
                                {
                                    "#2563EB",
                                    "#16A34A",
                                    "#F59E0B",
                                    "#7C3AED",
                                    "#0891B2",
                                    "#DC2626",
                                    "#4F46E5",
                                    "#65A30D",
                                    "#EA580C"
                                };

                                for (int i = 0;
                                     i < pieData.Count;
                                     i++)
                                {
                                    ReportChartItem item =
                                        pieData[i];

                                    string color =
                                        colors[i % colors.Length];

                                    legend.Item()
                                        .Row(legendRow =>
                                        {
                                            legendRow
                                                .ConstantItem(10)
                                                .Height(10)
                                                .Background(color);

                                            legendRow
                                                .RelativeItem()
                                                .PaddingLeft(6)
                                                .Text(
                                                    $"{item.Name} — R {item.BusinessValue:N2}")
                                                .FontSize(8);
                                        });
                                }
                            });
                    });
            });
        }

        // =========================================================
        // LINE GRAPH
        // =========================================================

        private static void ComposeLineChart(
            IContainer container,
            List<ReportTrendItem> data)
        {
            container.Column(column =>
            {
                column.Spacing(8);

                column.Item()
                    .Text("Business Value Trend — Line Graph")
                    .FontSize(14)
                    .SemiBold();

                if (data.Count == 0)
                {
                    column.Item()
                        .Text("No trend data is available.")
                        .FontColor("#64748B");

                    return;
                }

                column.Item()
                    .Height(210)
                    .Svg(size =>
                        BuildLineChartSvg(
                            data,
                            size.Width,
                            size.Height));
            });
        }

        // =========================================================
        // BREAKDOWN TABLE
        // =========================================================

        private static void ComposeDepartmentBreakdown(
            IContainer container,
            List<ReportChartItem> data)
        {
            container.Column(column =>
            {
                column.Spacing(8);

                column.Item()
                    .Text("Department Breakdown")
                    .FontSize(14)
                    .SemiBold();

                if (data.Count == 0)
                {
                    column.Item()
                        .Text("No department breakdown is available.")
                        .FontColor("#64748B");

                    return;
                }

                column.Item()
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2.2f);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn(1.4f);
                        });

                        table.Header(header =>
                        {
                            TableHeader(
                                header.Cell(),
                                "DEPARTMENT");

                            TableHeader(
                                header.Cell(),
                                "ACTIVITIES");

                            TableHeader(
                                header.Cell(),
                                "COMPLETED");

                            TableHeader(
                                header.Cell(),
                                "RATE");

                            TableHeader(
                                header.Cell(),
                                "VALUE");
                        });

                        foreach (ReportChartItem item in data)
                        {
                            TableCell(
                                table.Cell(),
                                item.Name);

                            TableCell(
                                table.Cell(),
                                item.Activities.ToString("N0"));

                            TableCell(
                                table.Cell(),
                                item.Completed.ToString("N0"));

                            TableCell(
                                table.Cell(),
                                $"{item.CompletionRate:N1}%");

                            TableCell(
                                table.Cell(),
                                $"R {item.BusinessValue:N2}");
                        }
                    });
            });
        }

        // =========================================================
        // DETAILED RECORDS
        // =========================================================

        private static void ComposeRecords(
            IContainer container,
            List<ReportDailyRecordDto> records)
        {
            container.Column(column =>
            {
                column.Spacing(8);

                column.Item()
                    .Text($"Report Records ({records.Count:N0})")
                    .FontSize(14)
                    .SemiBold();

                if (records.Count == 0)
                {
                    column.Item()
                        .Text(
                            "No records match the selected report filters.")
                        .FontColor("#64748B");

                    return;
                }

                column.Item()
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.1f);
                            columns.RelativeColumn(1.6f);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn();
                            columns.RelativeColumn(1.1f);
                        });

                        table.Header(header =>
                        {
                            TableHeader(
                                header.Cell(),
                                "DATE");

                            TableHeader(
                                header.Cell(),
                                "EMPLOYEE");

                            TableHeader(
                                header.Cell(),
                                "DEPARTMENT");

                            TableHeader(
                                header.Cell(),
                                "ACTIVITY");

                            TableHeader(
                                header.Cell(),
                                "QTY");

                            TableHeader(
                                header.Cell(),
                                "VALUE");
                        });

                        foreach (ReportDailyRecordDto record in records)
                        {
                            TableCell(
                                table.Cell(),
                                record.RecordDate
                                    .ToString("dd MMM yyyy"));

                            TableCell(
                                table.Cell(),
                                string.IsNullOrWhiteSpace(
                                    record.EmployeeName)
                                    ? record.EmployeeId
                                    : record.EmployeeName);

                            TableCell(
                                table.Cell(),
                                string.IsNullOrWhiteSpace(
                                    record.Department)
                                    ? "-"
                                    : record.Department);

                            TableCell(
                                table.Cell(),
                                string.IsNullOrWhiteSpace(
                                    record.ActivityType)
                                    ? "-"
                                    : record.ActivityType);

                            TableCell(
                                table.Cell(),
                                record.Quantity
                                    .ToString("N0"));

                            TableCell(
                                table.Cell(),
                                $"R {record.BusinessValue:N2}");
                        }
                    });
            });
        }

        // =========================================================
        // TABLE HELPERS
        // =========================================================

        private static void TableHeader(
            IContainer container,
            string text)
        {
            container
                .Background("#0F2747")
                .Padding(6)
                .Text(text)
                .FontSize(7)
                .SemiBold()
                .FontColor(Colors.White);
        }

        private static void TableCell(
            IContainer container,
            string text)
        {
            container
                .BorderBottom(1)
                .BorderColor("#E2E8F0")
                .Padding(6)
                .Text(text)
                .FontSize(7.5f)
                .FontColor("#0F172A");
        }

        // =========================================================
        // FOOTER
        // =========================================================

        private static void ComposeFooter(
            IContainer container)
        {
            container
                .BorderTop(1)
                .BorderColor("#E2E8F0")
                .PaddingTop(8)
                .Row(row =>
                {
                    row.RelativeItem()
                        .Text(
                            $"InsightFlow • Generated {DateTime.Now:dd MMM yyyy HH:mm}")
                        .FontSize(7)
                        .FontColor("#64748B");

                    row.ConstantItem(100)
                        .AlignRight()
                        .Text(text =>
                        {
                            text.Span("Page ")
                                .FontSize(7)
                                .FontColor("#64748B");

                            text.CurrentPageNumber()
                                .FontSize(7)
                                .FontColor("#64748B");

                            text.Span(" of ")
                                .FontSize(7)
                                .FontColor("#64748B");

                            text.TotalPages()
                                .FontSize(7)
                                .FontColor("#64748B");
                        });
                });
        }

        // =========================================================
        // STATISTICS
        // =========================================================

        private static ReportStatistics CalculateStatistics(
            List<ReportDailyRecordDto> records)
        {
            int total =
                records.Count;

            int completed =
                records.Count(
                    record =>
                        string.Equals(
                            record.Status?.Trim(),
                            "Completed",
                            StringComparison.OrdinalIgnoreCase));

            int inProgress =
                records.Count(
                    record =>
                        string.Equals(
                            record.Status?.Trim(),
                            "In Progress",
                            StringComparison.OrdinalIgnoreCase));

            int pending =
                records.Count(
                    record =>
                        string.Equals(
                            record.Status?.Trim(),
                            "Pending",
                            StringComparison.OrdinalIgnoreCase));

            int onHold =
                records.Count(
                    record =>
                        string.Equals(
                            record.Status?.Trim(),
                            "On Hold",
                            StringComparison.OrdinalIgnoreCase));

            int contributors =
                records
                    .Where(record =>
                        record.EmployeeAccountId > 0)
                    .Select(record =>
                        record.EmployeeAccountId)
                    .Distinct()
                    .Count();

            long totalQuantity =
                records.Sum(
                    record =>
                        (long)record.Quantity);

            decimal businessValue =
                records.Sum(
                    record =>
                        record.BusinessValue);

            double completionRate =
                total == 0
                    ? 0
                    : (double)completed /
                      total * 100;

            return new ReportStatistics
            {
                TotalActivities = total,
                Completed = completed,
                CompletionRate = completionRate,
                BusinessValue = businessValue,
                Contributors = contributors,
                TotalQuantity = totalQuantity,
                InProgress = inProgress,
                PendingHold = pending + onHold
            };
        }

        // =========================================================
        // DEPARTMENT DATA
        // =========================================================

        private static List<ReportChartItem>
            BuildDepartmentData(
                List<ReportDailyRecordDto> records)
        {
            return records
                .GroupBy(
                    record =>
                        string.IsNullOrWhiteSpace(
                            record.Department)
                            ? "Unknown Department"
                            : record.Department.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    int activities =
                        group.Count();

                    int completed =
                        group.Count(
                            record =>
                                string.Equals(
                                    record.Status?.Trim(),
                                    "Completed",
                                    StringComparison.OrdinalIgnoreCase));

                    decimal businessValue =
                        group.Sum(
                            record =>
                                record.BusinessValue);

                    double completionRate =
                        activities == 0
                            ? 0
                            : (double)completed /
                              activities * 100;

                    return new ReportChartItem
                    {
                        Name = group.Key,
                        Activities = activities,
                        Completed = completed,
                        CompletionRate = completionRate,
                        BusinessValue = businessValue
                    };
                })
                .OrderByDescending(
                    item =>
                        item.Activities)
                .ThenBy(
                    item =>
                        item.Name)
                .ToList();
        }

        // =========================================================
        // TREND DATA
        // =========================================================

        private static List<ReportTrendItem>
            BuildTrendData(
                List<ReportDailyRecordDto> records)
        {
            return records
                .GroupBy(
                    record =>
                        record.RecordDate.Date)
                .Select(group =>
                    new ReportTrendItem
                    {
                        Date = group.Key,

                        BusinessValue =
                            group.Sum(
                                record =>
                                    record.BusinessValue),

                        Activities =
                            group.Count()
                    })
                .OrderBy(
                    item =>
                        item.Date)
                .ToList();
        }

        // =========================================================
        // PIE CHART SVG
        // =========================================================

        private static string BuildPieChartSvg(
            List<ReportChartItem> data,
            float width,
            float height)
        {
            double total =
                data.Sum(
                    item =>
                        (double)item.BusinessValue);

            if (total <= 0)
            {
                return
                    $"""
                    <svg xmlns="http://www.w3.org/2000/svg"
                         width="{width}"
                         height="{height}"
                         viewBox="0 0 {width} {height}">
                        <text x="10"
                              y="20"
                              font-size="12"
                              fill="#64748B">
                            No chart data
                        </text>
                    </svg>
                    """;
            }

            string[] colors =
            {
                "#2563EB",
                "#16A34A",
                "#F59E0B",
                "#7C3AED",
                "#0891B2",
                "#DC2626",
                "#4F46E5",
                "#65A30D",
                "#EA580C"
            };

            double centerX =
                width / 2.0;

            double centerY =
                height / 2.0;

            double radius =
                Math.Max(
                    20,
                    Math.Min(width, height) /
                    2.0 - 10);

            double startAngle =
                -90;

            System.Text.StringBuilder svg =
                new();

            svg.Append(
                $"""
                <svg xmlns="http://www.w3.org/2000/svg"
                     width="{width}"
                     height="{height}"
                     viewBox="0 0 {width} {height}">
                """);

            for (int i = 0;
                 i < data.Count;
                 i++)
            {
                ReportChartItem item =
                    data[i];

                double percentage =
                    (double)item.BusinessValue /
                    total;

                double sweepAngle =
                    percentage * 360.0;

                double endAngle =
                    startAngle + sweepAngle;

                string color =
                    colors[i % colors.Length];

                if (percentage >= 0.999999)
                {
                    svg.Append(
                        $"""
                        <circle
                            cx="{centerX.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                            cy="{centerY.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                            r="{radius.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                            fill="{color}" />
                        """);
                }
                else
                {
                    (double startX, double startY) =
                        PolarToCartesian(
                            centerX,
                            centerY,
                            radius,
                            startAngle);

                    (double endX, double endY) =
                        PolarToCartesian(
                            centerX,
                            centerY,
                            radius,
                            endAngle);

                    int largeArcFlag =
                        sweepAngle > 180
                            ? 1
                            : 0;

                    svg.Append(
                        $"""
                        <path
                            d="M {centerX.ToString(System.Globalization.CultureInfo.InvariantCulture)}
                               {centerY.ToString(System.Globalization.CultureInfo.InvariantCulture)}
                               L {startX.ToString(System.Globalization.CultureInfo.InvariantCulture)}
                               {startY.ToString(System.Globalization.CultureInfo.InvariantCulture)}
                               A {radius.ToString(System.Globalization.CultureInfo.InvariantCulture)}
                               {radius.ToString(System.Globalization.CultureInfo.InvariantCulture)}
                               0 {largeArcFlag} 1
                               {endX.ToString(System.Globalization.CultureInfo.InvariantCulture)}
                               {endY.ToString(System.Globalization.CultureInfo.InvariantCulture)}
                               Z"
                            fill="{color}"
                            stroke="#FFFFFF"
                            stroke-width="1" />
                        """);
                }

                startAngle =
                    endAngle;
            }

            svg.Append("</svg>");

            return svg.ToString();
        }

        // =========================================================
        // LINE GRAPH SVG
        // =========================================================

        private static string BuildLineChartSvg(
            List<ReportTrendItem> data,
            float width,
            float height)
        {
            double left =
                45;

            double right =
                15;

            double top =
                15;

            double bottom =
                35;

            double chartWidth =
                Math.Max(
                    10,
                    width - left - right);

            double chartHeight =
                Math.Max(
                    10,
                    height - top - bottom);

            double maximum =
                Math.Max(
                    1,
                    data.Max(
                        item =>
                            (double)item.BusinessValue));

            System.Text.StringBuilder svg =
                new();

            svg.Append(
                $"""
                <svg xmlns="http://www.w3.org/2000/svg"
                     width="{width}"
                     height="{height}"
                     viewBox="0 0 {width} {height}">

                    <rect
                        x="0"
                        y="0"
                        width="{width}"
                        height="{height}"
                        fill="#FFFFFF"/>

                    <line
                        x1="{left}"
                        y1="{top}"
                        x2="{left}"
                        y2="{top + chartHeight}"
                        stroke="#94A3B8"
                        stroke-width="1"/>

                    <line
                        x1="{left}"
                        y1="{top + chartHeight}"
                        x2="{left + chartWidth}"
                        y2="{top + chartHeight}"
                        stroke="#94A3B8"
                        stroke-width="1"/>
                """);

            // Horizontal grid lines
            for (int i = 0;
                 i <= 4;
                 i++)
            {
                double y =
                    top +
                    chartHeight -
                    chartHeight * i / 4.0;

                double value =
                    maximum * i / 4.0;

                svg.Append(
                    $"""
                    <line
                        x1="{left}"
                        y1="{y.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                        x2="{(left + chartWidth).ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                        y2="{y.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                        stroke="#E2E8F0"
                        stroke-width="1"/>

                    <text
                        x="{left - 5}"
                        y="{(y + 3).ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                        text-anchor="end"
                        font-size="7"
                        fill="#64748B">
                        {value:N0}
                    </text>
                    """);
            }

            List<(double X, double Y)> points =
                new();

            for (int i = 0;
                 i < data.Count;
                 i++)
            {
                double x;

                if (data.Count == 1)
                {
                    x =
                        left +
                        chartWidth / 2.0;
                }
                else
                {
                    x =
                        left +
                        chartWidth *
                        i /
                        (data.Count - 1.0);
                }

                double y =
                    top +
                    chartHeight -
                    ((double)data[i].BusinessValue /
                     maximum *
                     chartHeight);

                points.Add(
                    (x, y));
            }

            string polylinePoints =
                string.Join(
                    " ",
                    points.Select(
                        point =>
                            $"{point.X.ToString(System.Globalization.CultureInfo.InvariantCulture)},{point.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));

            svg.Append(
                $"""
                <polyline
                    points="{polylinePoints}"
                    fill="none"
                    stroke="#2563EB"
                    stroke-width="2.5"
                    stroke-linejoin="round"
                    stroke-linecap="round"/>
                """);

            for (int i = 0;
                 i < points.Count;
                 i++)
            {
                (double x, double y) =
                    points[i];

                svg.Append(
                    $"""
                    <circle
                        cx="{x.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                        cy="{y.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                        r="3"
                        fill="#2563EB"/>

                    <text
                        x="{x.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                        y="{(top + chartHeight + 15).ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                        text-anchor="middle"
                        font-size="6.5"
                        fill="#64748B">
                        {data[i].Date:dd MMM}
                    </text>
                    """);
            }

            svg.Append("</svg>");

            return svg.ToString();
        }

        // =========================================================
        // PIE GEOMETRY
        // =========================================================

        private static (
            double X,
            double Y)
            PolarToCartesian(
                double centerX,
                double centerY,
                double radius,
                double angleDegrees)
        {
            double angleRadians =
                angleDegrees *
                Math.PI /
                180.0;

            return (
                centerX +
                radius *
                Math.Cos(angleRadians),

                centerY +
                radius *
                Math.Sin(angleRadians));
        }

        // =========================================================
        // INTERNAL REPORT MODELS
        // =========================================================

        private class ReportStatistics
        {
            public int TotalActivities { get; set; }

            public int Completed { get; set; }

            public double CompletionRate { get; set; }

            public decimal BusinessValue { get; set; }

            public int Contributors { get; set; }

            public long TotalQuantity { get; set; }

            public int InProgress { get; set; }

            public int PendingHold { get; set; }
        }

        private class ReportChartItem
        {
            public string Name { get; set; } =
                string.Empty;

            public int Activities { get; set; }

            public int Completed { get; set; }

            public double CompletionRate { get; set; }

            public decimal BusinessValue { get; set; }
        }

        private class ReportTrendItem
        {
            public DateTime Date { get; set; }

            public decimal BusinessValue { get; set; }

            public int Activities { get; set; }
        }
    }
}