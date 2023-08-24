package gr.jimmys.jimmysfoodzilla.utils;

public class Tuple3<K, V, Z> {

    private K first;
    private V second;

    private Z third;

    public Tuple3(K first, V second, Z third){
        this.first = first;
        this.second = second;
        this.third = third;
    }

    public K getFirst() {
        return first;
    }

    public void setFirst(K first) {
        this.first = first;
    }

    public V getSecond() {
        return second;
    }

    public void setSecond(V second) {
        this.second = second;
    }

    public Z getThird() {
        return third;
    }

    public void setThird(Z third) {
        this.third = third;
    }
}
