/// <summary>
/// Room Divisi Botanist -- tempat kerja & spawn point employee ber-Division = EmployeeDivision.Botanist.
/// Spesialisasi : Feed & Harvest (lihat Employee.CalculateFeedDuration/CalculateHarvestDuration --
/// employee Botanist mengerjakan keduanya dengan durasi normal, sementara Research kena
/// penalti 5x lebih lama).
///
/// Semua employee yang di-spawn dari list "Employees To Spawn" di Inspector room ini otomatis
/// jadi EmployeeDivision.Botanist (lihat EmployeeDivisionType, DivisionRoom.SpawnEmployees).
///
/// Belum ada behavior tambahan spesifik selain identitas tipe divisi -- tambahkan di sini
/// kalau nanti mau ada fitur khusus room Botanist (mis. bonus growth buat monster nearby,
/// dsb).
/// </summary>
public class DivisionBotanist : DivisionRoom
{
    protected override EmployeeDivision EmployeeDivisionType => EmployeeDivision.Botanist;
}