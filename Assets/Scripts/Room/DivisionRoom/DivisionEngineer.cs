/// <summary>
/// Room Divisi Engineer -- tempat kerja & spawn point employee ber-Division = EmployeeDivision.Engineer.
/// Spesialisasi : Perbaikan Listrik (Fix Electricity).
/// </summary>
public class DivisionEngineer : DivisionRoom
{
    protected override EmployeeDivision EmployeeDivisionType => EmployeeDivision.Engineer;
}
