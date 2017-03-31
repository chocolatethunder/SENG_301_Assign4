using System;
using System.Collections.Generic;
using Frontend4;
using Frontend4.Hardware;


/**
 * Represents vending machines, fully configured and with all software
 * installed.
 * 
 */
public class VendingMachine {

    private HardwareFacade hardwareFacade;
    public HardwareFacade Hardware {
        get {
            return this.hardwareFacade;
        }
    }


    /**
     * Creates a standard arrangement for the vending machine. All the
     * components are created and interconnected. The hardware is initially
     * empty. The product kind names and costs are initialized to &quot; &quot;
     * and 1 respectively.
     * 
     * @param coinKinds
     *            The values (in cents) of each kind of coin. The order of the
     *            kinds is maintained. One coin rack is produced for each kind.
     *            Each kind must have a unique, positive value.
     * @param selectionButtonCount
     *            The number of selection buttons on the machine. Must be
     *            positive.
     * @param coinRackCapacity
     *            The maximum capacity of each coin rack in the machine. Must be
     *            positive.
     * @param productRackCapacity
     *            The maximum capacity of each product rack in the machine. Must
     *            be positive.
     * @param receptacleCapacity
     *            The maximum capacity of the coin receptacle, storage bin, and
     *            delivery chute. Must be positive.
     * @throws IllegalArgumentException
     *             If any of the arguments is null, or the size of productCosts
     *             and productNames differ.
     */
    public VendingMachine(Cents[] coinKinds, int selectionButtonCount, int coinRackCapacity, int productRackCapacity, int receptacleCapacity) {
	    this.hardwareFacade = new HardwareFacade(coinKinds, selectionButtonCount, coinRackCapacity, productRackCapacity, receptacleCapacity);


        /* YOU CAN BUILD AND INSTALL THE HARDWARE HERE */

        PaymentFacade payment       = new PaymentFacade(this.hardwareFacade);
        CommunicationFacade comms   = new CommunicationFacade(this.hardwareFacade);
        ProductFacade prod          = new ProductFacade(this.hardwareFacade);

    }

}

public class HardwareLogic {

    // This class talks to the facades
    public HardwareLogic() {

    }

}


/* Usage
 * 
 * 
 */
public class PaymentFacade {

    // Subscribe to events coming from HardwareFacade

    // Local copy of the hardware facade
    private HardwareFacade hw;

    // Variable used in the "API"

    // Local variables used to process logic

    public PaymentFacade(HardwareFacade hardwarefacade) {
        this.hw = hardwarefacade;
    }

    // INTERNAL PROCESSING METHODS

    // INTERFACING METHODS

    public void insertCoin(Cents coin) {

    }

    public void currentCredit() {

    }

}

/* Usage
 * 
 * 
 */
public class CommunicationFacade {

    // Subscribe to events coming from HardwareFacade

    // Local copy of the hardware facade
    private HardwareFacade hw;

    // Variable used in the "API"

    // Local variables used to process logic

    public CommunicationFacade(HardwareFacade hardwarefacade) {
        this.hw = hardwarefacade;
    }

    // INTERNAL PROCESSING METHODS

    // INTERFACING METHODS

}

/* Usage
 * 
 * 
 */
public class ProductFacade {

    // Subscribe to events coming from HardwareFacade

    // Local copy of the hardware facade
    private HardwareFacade hw;

    // Variable used in the "API"

    // Local variables used to process logic

    public ProductFacade(HardwareFacade hardwarefacade) {
        this.hw = hardwarefacade;
    }

    // INTERNAL PROCESSING METHODS

    // INTERFACING METHODS

}
