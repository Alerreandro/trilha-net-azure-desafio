using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using TrilhaNetAzureDesafio.Context;
using TrilhaNetAzureDesafio.Models;

namespace TrilhaNetAzureDesafio.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FuncionarioController : ControllerBase
{
    private readonly RHContext _dbContext;
    private readonly string _azureConnection;
    private readonly string _tableName;

    public FuncionarioController(RHContext dbContext, IConfiguration config)
    {
        _dbContext = dbContext;
        _azureConnection = config["ConnectionStrings:SAConnectionString"];
        _tableName = config["ConnectionStrings:AzureTableName"];
    }

    private TableClient ObterClienteTabela()
    {
        var serviceClient = new TableServiceClient(_azureConnection);
        var client = serviceClient.GetTableClient(_tableName);
        client.CreateIfNotExists();
        return client;
    }

    [HttpGet("{id}")]
    public IActionResult BuscarPorId(int id)
    {
        var func = _dbContext.Funcionarios.Find(id);
        if (func == null)
            return NotFound(new { Mensagem = "Funcionário não encontrado." });

        return Ok(func);
    }

    [HttpPost]
    public IActionResult Adicionar(Funcionario funcionario)
    {
        if (funcionario == null)
            return BadRequest();

        _dbContext.Funcionarios.Add(funcionario);
        _dbContext.SaveChanges();

        var tabela = ObterClienteTabela();
        var log = new FuncionarioLog(
            funcionario, 
            TipoAcao.Inclusao, 
            funcionario.Departamento, 
            Guid.NewGuid().ToString()
        );

        tabela.UpsertEntity(log);

        return CreatedAtAction(nameof(BuscarPorId), new { id = funcionario.Id }, funcionario);
    }

    [HttpPut("{id}")]
    public IActionResult Modificar(int id, Funcionario funcionarioAtualizado)
    {
        var funcExistente = _dbContext.Funcionarios.Find(id);
        if (funcExistente == null)
            return NotFound(new { Mensagem = "Funcionário não encontrado." });

        funcExistente.Nome = funcionarioAtualizado.Nome;
        funcExistente.DataAdmissao = funcionarioAtualizado.DataAdmissao;
        funcExistente.Salario = funcionarioAtualizado.Salario;
        funcExistente.Departamento = funcionarioAtualizado.Departamento;
        funcExistente.Ramal = funcionarioAtualizado.Ramal;

        _dbContext.Entry(funcExistente).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
        _dbContext.SaveChanges();

        var tabela = ObterClienteTabela();
        var log = new FuncionarioLog(
            funcExistente,
            TipoAcao.Atualizacao,
            funcExistente.Departamento,
            Guid.NewGuid().ToString()
        );

        tabela.UpsertEntity(log);

        return Ok(funcExistente);
    }

    [HttpDelete("{id}")]
    public IActionResult Remover(int id)
    {
        var func = _dbContext.Funcionarios.Find(id);
        if (func == null)
            return NotFound(new { Mensagem = "Funcionário não encontrado." });

        _dbContext.Funcionarios.Remove(func);
        _dbContext.SaveChanges();

        var tabela = ObterClienteTabela();
        var log = new FuncionarioLog(
            func,
            TipoAcao.Remocao,
            func.Departamento,
            Guid.NewGuid().ToString()
        );

        tabela.UpsertEntity(log);

        return NoContent();
    }
}
    