package gr.jimmys.jimmysfoodzilla.utils;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@NoArgsConstructor
@AllArgsConstructor
@Data
public class Holder<T> {
    private T value;
}
