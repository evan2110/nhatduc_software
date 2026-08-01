using NhatDucSoftware.Core.Services;

namespace NhatDucSoftware.Core.Tests;

public class PaymentFinalizeRemainingTests
{
    [Fact]
    public void GetCarryAmountForClass_UsesRemainingAfterOverpaymentCredit_NotRawDueMinusPaid()
    {
        var rows = new List<StudentClassTuitionRow>
        {
            new()
            {
                ClassId = 1,
                ClassName = "A",
                GrossAttendance = 100_000m,
                TotalDue = 100_000m,
                Paid = 150_000m,
                Remaining = Math.Max(0, 100_000m - 150_000m)
            },
            new()
            {
                ClassId = 2,
                ClassName = "B",
                GrossAttendance = 50_000m,
                TotalDue = 50_000m,
                Paid = 0m,
                Remaining = 50_000m
            }
        };

        PaymentServiceInternals.ApplyOverpaymentCredit(rows);

        // UI remaining for B is 0 after credit from A; old finalize used TotalDue - Paid = 50_000.
        Assert.Equal(0m, rows.Single(r => r.ClassId == 2).Remaining);
        Assert.Equal(50_000m, rows.Single(r => r.ClassId == 2).TotalDue - rows.Single(r => r.ClassId == 2).Paid);

        Assert.Equal(0m, PaymentServiceInternals.GetCarryAmountForClass(rows, 2));
        Assert.Equal(0m, PaymentServiceInternals.GetCarryAmountForClass(rows, 1));
    }

    [Fact]
    public void GetCarryAmountForClass_UsesRemainingAfterUnallocatedPayment()
    {
        var rows = new List<StudentClassTuitionRow>
        {
            new()
            {
                ClassId = 1,
                ClassName = "A",
                GrossAttendance = 84_000m,
                TotalDue = 84_000m,
                Paid = 0m,
                Remaining = 84_000m
            }
        };

        PaymentServiceInternals.ApplyUnallocatedPayments(rows, 84_000m);

        Assert.Equal(0m, rows[0].Remaining);
        Assert.Equal(0m, PaymentServiceInternals.GetCarryAmountForClass(rows, 1));
    }
}
