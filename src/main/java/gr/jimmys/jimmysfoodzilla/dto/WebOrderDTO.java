package gr.jimmys.jimmysfoodzilla.dto;

import jakarta.validation.Valid;
import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;

public record WebOrderDTO(
        @NotNull @NotEmpty @Size(max = 100) @Valid WebOrderItemDTO[] order
) {}
