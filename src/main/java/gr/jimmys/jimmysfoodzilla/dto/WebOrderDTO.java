package gr.jimmys.jimmysfoodzilla.dto;

import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;

public record WebOrderDTO(
        @NotNull @NotEmpty @Size(max = 50) @Valid WebOrderItemDTO[] order,
        @NotBlank @Size(max = 100) String userId
) {}
