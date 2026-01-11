using PurrNet;
using UnityEngine;

public class PlayerAvatar : PlayerIdentity<PlayerAvatar>
{
    public SyncVar<int> seatIndex = new(-1);

    public int SeatIndex => seatIndex.value;

    public void Server_SetSeat(int seat)
    {
        seatIndex.value = seat;
    }
}
