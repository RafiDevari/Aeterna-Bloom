/// <summary>
/// Room Divisi Clerk -- tempat kerja & spawn point employee ber-Division = EmployeeDivision.Clerk.
/// </summary>
public class DivisionClerk : DivisionRoom
{
    protected override EmployeeDivision EmployeeDivisionType => EmployeeDivision.Clerk;
}
