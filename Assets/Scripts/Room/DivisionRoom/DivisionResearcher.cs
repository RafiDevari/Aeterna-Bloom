/// <summary>
/// Room Divisi Researcher -- tempat kerja & spawn point employee ber-Division = EmployeeDivision.Researcher.
/// Spesialisasi : Research (lihat Employee.CalculateResearchDuration -- employee Researcher
/// mengerjakan Research dengan durasi normal, sementara Feed & Harvest kena penalti 5x
/// lebih lama).
///
/// Semua employee yang di-spawn dari list "Employees To Spawn" di Inspector room ini otomatis
/// jadi EmployeeDivision.Researcher (lihat EmployeeDivisionType, DivisionRoom.SpawnEmployees).
///
/// Belum ada behavior tambahan spesifik selain identitas tipe divisi -- tambahkan di sini
/// kalau nanti mau ada fitur khusus room Researcher (mis. bonus research speed pasif, dsb).
/// </summary>
public class DivisionResearcher : DivisionRoom
{
    protected override EmployeeDivision EmployeeDivisionType => EmployeeDivision.Researcher;
}