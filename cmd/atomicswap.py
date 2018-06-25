from contract import Contract

class AtomicSwap(Contract):
   
    owner = None

    def __init__(self, w3, abi, bin=None, owner=None, args=None, address=None):
        self.set_w3(w3)
        self.bin = bin
        self.abi = abi

        if address:
            self.from_address(address)
        else:
            self.deploy(owner, args)
        
        self.owner = owner


    def redeem(self, sender, secret):
        ret = self.instance.transact(transaction={'from': sender}).redeem(secret)
        return ret	


    def refund(self, sender):
        ret = self.instance.transact(transaction={'from': sender}).refund()
        return ret		


    def init_timestamp(self):
        ret = self.instance.call().initTimestamp()
        return ret


    def refund_time(self):
        ret = self.instance.call().refundTime()
        return ret


    def hashed_secret(self):
        ret = self.instance.call().hashedSecret()
        return ret


    def initiator(self):
        ret = self.instance.call().initiator()
        return ret


    def participant(self):
        ret = self.instance.call().participant()
        return ret


    def value(self):
        ret = self.instance.call().value()
        return ret


    def emptied(self):
        ret = self.instance.call().emptied()
        return ret


    def state(self):
        ret = self.instance.call().state()
        return ret
