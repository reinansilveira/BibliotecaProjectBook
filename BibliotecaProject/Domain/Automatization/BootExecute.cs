using BibliotecaProject.Domain.Entities;
using BibliotecaProject.Domain.Services;
using Quartz;

namespace BibliotecaProject.Exceptions;

public class BootExecute : IJob
{
    private readonly BlackListBoot _blackListBoot;

    public BootExecute(BlackListBoot blackListBoot)
    {
        _blackListBoot = blackListBoot;
    }
    public async Task Execute(IJobExecutionContext context)
    {
        await _blackListBoot.VerificarLogin();
    }
}