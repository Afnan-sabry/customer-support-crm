using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Knowledge.Commands;
using CustomerSupport.Application.Knowledge.DTOs;
using CustomerSupport.Application.Knowledge.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class KnowledgeController : ControllerBase
{
    private readonly IMediator _mediator;

    public KnowledgeController(IMediator mediator) => _mediator = mediator;

    [HttpGet("categories")]
    [Authorize(Policy = "Permission:knowledgebase.view")]
    public async Task<ActionResult<List<KnowledgeCategoryDto>>> GetCategories([FromQuery] bool? isActive)
        => Ok(await _mediator.Send(new GetKnowledgeCategoriesQuery(isActive)));

    [HttpPost("categories")]
    [Authorize(Policy = "Permission:knowledgebase.manage")]
    public async Task<ActionResult<KnowledgeCategoryDto>> CreateCategory(CreateKnowledgeCategoryCommand command)
        => CreatedAtAction(null, await _mediator.Send(command));

    [HttpPut("categories/{id:guid}")]
    [Authorize(Policy = "Permission:knowledgebase.manage")]
    public async Task<ActionResult<KnowledgeCategoryDto>> UpdateCategory(Guid id, UpdateKnowledgeCategoryCommand command)
    {
        if (id != command.Id) return BadRequest();
        return Ok(await _mediator.Send(command));
    }

    [HttpDelete("categories/{id:guid}")]
    [Authorize(Policy = "Permission:knowledgebase.manage")]
    public async Task<ActionResult<Result>> DeleteCategory(Guid id)
    {
        var result = await _mediator.Send(new DeleteKnowledgeCategoryCommand(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet("articles")]
    [Authorize(Policy = "Permission:knowledgebase.view")]
    public async Task<ActionResult<PaginatedList<KnowledgeArticleDto>>> GetArticles(
        [FromQuery] Guid? categoryId, [FromQuery] bool? isPublished,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _mediator.Send(new GetKnowledgeArticlesQuery(categoryId, isPublished, page, pageSize)));

    [HttpGet("articles/{id:guid}")]
    [Authorize(Policy = "Permission:knowledgebase.view")]
    public async Task<ActionResult<KnowledgeArticleDetailDto>> GetArticleById(Guid id)
        => Ok(await _mediator.Send(new GetKnowledgeArticleByIdQuery(id)));

    [HttpGet("articles/search")]
    [Authorize(Policy = "Permission:knowledgebase.view")]
    public async Task<ActionResult<PaginatedList<KnowledgeArticleDto>>> SearchArticles(
        [FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _mediator.Send(new SearchKnowledgeArticlesQuery(query, page, pageSize)));

    [HttpPost("articles")]
    [Authorize(Policy = "Permission:knowledgebase.manage")]
    public async Task<ActionResult<KnowledgeArticleDto>> CreateArticle(CreateKnowledgeArticleCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetArticleById), new { id = result.Id }, result);
    }

    [HttpPut("articles/{id:guid}")]
    [Authorize(Policy = "Permission:knowledgebase.manage")]
    public async Task<ActionResult<KnowledgeArticleDetailDto>> UpdateArticle(Guid id, UpdateKnowledgeArticleCommand command)
    {
        if (id != command.Id) return BadRequest();
        return Ok(await _mediator.Send(command));
    }

    [HttpDelete("articles/{id:guid}")]
    [Authorize(Policy = "Permission:knowledgebase.manage")]
    public async Task<ActionResult<Result>> DeleteArticle(Guid id)
    {
        var result = await _mediator.Send(new DeleteKnowledgeArticleCommand(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
