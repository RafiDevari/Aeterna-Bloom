public interface EmployeeTask
{
    void Start(Employee employee, System.Action onComplete, System.Action onFail);
    void Cancel();
}