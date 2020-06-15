# Profit Center request policy 

The initial creation of a profit center is one of few elements of logical security within MAP that PRM is responsible for.

New profit centers in the production or internal environment must be requested via the Profit Center Request issue template, located in the `.github` directory of this repository.  To make a request, please open an issue in this repository using that template and assign it to either a PRM Principal and/or the PRM security manager.

The request must be approved by a PRM Principal or the PRM security manager before the Profit Center is created. The request cannot be made and approved by the same person. The profit center can be created by the requestor, but only after it is approved by a PRM Principal or the security manager.

Additionally, the initial user assigned to the profit center must be a Milliman employee. That individual is responsible for the creation of clients and assigning users. 

### MOU required for Production Profit Centers

Before a Profit Center is created in the production environment, the client must sign a Memorandum of Understanding for MAP, which establishes their responsibilities for protecting the security and integrity of MAP.

Profit Centers can be created in the Internal environment (`indy-map`) for prospective clients, for testing/demo purposes. Those Profit Centers do not require an MOU.

### Adding Profit Center administrators

Any additional profit center administrators must be requested by a profit center administrator or PRM account manager, by sending an email to `prm.security@milliman.com`.
